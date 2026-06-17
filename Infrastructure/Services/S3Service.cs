using Amazon.S3.Model;
using MIO.API.Settings;
using MIO.Core.Enums;

namespace MIO2.Infrastructure.Services
{

    using Amazon.S3;
    using Amazon.S3.Transfer;
    using Microsoft.Extensions.Options;
    using MIO2.API.Settings;
    using MIO2.Core.Interfaces;

    public class S3Service: IS3Service
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;
        private readonly string _regionSystemName;
        private readonly S3Settings _settings;
        public S3Service(IOptions<S3Settings>options)
        {
            _settings = options.Value;  

            _bucket = _settings.BucketName;
            _regionSystemName = _settings.Region;

            var region = Amazon.RegionEndpoint.GetBySystemName(_settings.Region);
            
            _s3 =new AmazonS3Client(
                _settings.AccessKey,
                _settings.SecretKey,
                region 
                );
        }

        private void IsAllowedType( string contentType, MediaCategory category)
        {
            if (!AllowedTypes.TryGetValue(category, out var allowedForCategory))
                throw new AppException("Invalid media category.", StatusCodes.Status400BadRequest);

            if (!allowedForCategory.Contains(contentType))
                throw new AppException($"Content type '{contentType}' is not allowed for this category.", StatusCodes.Status400BadRequest);

          
        }

        private void IsSizeValid( string contentType, long size)
        {
            
            if (contentType.StartsWith("image/") && size > 5 * 1024 * 1024)
                throw new AppException("Image size must not exceed 5MB.", StatusCodes.Status400BadRequest);

            if (contentType.StartsWith("video/") && size > 200 * 1024 * 1024)
                throw new AppException("Video size must not exceed 200MB.", StatusCodes.Status400BadRequest);

          
        }
        
        public (string fileKey,string preSignedUrl) CreateUploadURL(string fileName, string contentType,
                                                                     MediaCategory category, long size)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new AppException("FileName is required.", StatusCodes.Status400BadRequest);

            if (string.IsNullOrWhiteSpace(contentType))
                throw new AppException("ContentType is required.", StatusCodes.Status400BadRequest);

            // Check category is in AllowedTypes and contentType is allowed for it
            IsAllowedType(contentType, category);

            // Size limits

            IsSizeValid(contentType, size);

            string prefix = category switch
            {
                MediaCategory.UserProfile => "users/profile/",
                MediaCategory.QuestionImage => "questions/images/",
                MediaCategory.SolutionImage => "solutions/images/",
                MediaCategory.GalleryImage => "gallery/images/",
                MediaCategory.GalleryVideo => "gallery/videos/",
                MediaCategory.AnnouncementImage => "announcements/images/",
                MediaCategory.SubmissionAttachment => "submissions/",
                _ => throw new AppException(
                    "Invalid media category.",
                    StatusCodes.Status400BadRequest)
            };
            
                
            var ext = Path.GetExtension(fileName);

            var fileKey = $"{prefix}{Guid.NewGuid()}{ext}";

          
            var request = new Amazon.S3.Model.GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = fileKey,
                Verb = Amazon.S3.HttpVerb.PUT,            
                Expires = DateTime.UtcNow.AddMinutes(10),
                ContentType = contentType                  
            };

            var preSignedUrl = _s3.GetPreSignedURL(request);
            
            return (fileKey, preSignedUrl);
            
        }
        
        private static readonly Dictionary<MediaCategory, string[]> AllowedTypes = new()
        {
            { MediaCategory.UserProfile,          new[] { "image/jpeg", "image/png", "image/webp" } },
            { MediaCategory.QuestionImage,        new[] { "image/jpeg", "image/png", "image/webp" } },
            { MediaCategory.SolutionImage,        new[] { "image/jpeg", "image/png", "image/webp" } },
            { MediaCategory.GalleryImage,         new[] { "image/jpeg", "image/png", "image/webp" } },
            { MediaCategory.AnnouncementImage,    new[] { "image/jpeg", "image/png", "image/webp" } },
            { MediaCategory.GalleryVideo,         new[] { "video/mp4", "video/webm" } },
            { MediaCategory.SubmissionAttachment, new[] { "image/jpeg", "image/png", "image/webp", "video/mp4", "video/webm" } },
        };
        
        
        public string GenerateSignedUrl(string fileKey)
        {
            var request = new Amazon.S3.Model.GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = fileKey,
                Expires = DateTime.UtcNow.AddMinutes(10) 
            };

            return _s3.GetPreSignedURL(request);
        }

        public async Task<bool> IsMediaExists(string key)
        {
            try
            {
                var response = await _s3.GetObjectMetadataAsync(_bucket, key);

                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                throw;
            }
        }

        public string GetPublicUrl(string key)
        {
            return $"https://{_bucket}.s3.{_regionSystemName}.amazonaws.com/{key}";
        }

         
        public async Task<bool> DeleteMediaAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new AppException("FileKey is required.", StatusCodes.Status400BadRequest);

            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _bucket,
                    Key = key
                };

                await _s3.DeleteObjectAsync(request);

                return true;
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
              
                throw new AppException(
                    $"Failed to delete object from S3: {ex.Message}",
                    StatusCodes.Status500InternalServerError);
               
            }
            
        }
    }
}
