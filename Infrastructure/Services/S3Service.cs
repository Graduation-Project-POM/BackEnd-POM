using Amazon.S3.Model;
using MIO.API.Settings;

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
        
        
        public (string fileKey,string preSignedUrl) CreateUploadURL(string fileName,
            string contentType)
        {
            if (String.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("FileName is required.");

            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("ContentType is required.");

            string prefix="";

            if (contentType.StartsWith("image/",StringComparison.OrdinalIgnoreCase))
                prefix = "images/";
            else if (contentType.StartsWith("video/",StringComparison.OrdinalIgnoreCase))
                prefix = "videos/";
            else
            {
                throw new AppException(
                    "This content type is not supported.",
                    StatusCodes.Status400BadRequest);
            }
                
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
