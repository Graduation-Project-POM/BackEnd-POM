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


        //public async Task<string> UploadPicture(IFormFile file)
        //{
        //    var fileName = $"{Guid.NewGuid()}_{file.FileName}";

        //    using var stream = file.OpenReadStream();

        //    var request = new TransferUtilityUploadRequest
        //    {

        //        InputStream = stream,
        //        Key = fileName,
        //        BucketName = _bucket,
        //        ContentType = file.ContentType,

        //    };
        //    var transfer = new TransferUtility(_s3);

        //    await transfer.UploadAsync(request);

        //    return fileName;
        //}


        public async Task<(string url, string fileKey)> UploadPicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty.");


            var ext = Path.GetExtension(file.FileName);

            var fileKey = $"{Guid.NewGuid()}{ext}";

            using var stream=file.OpenReadStream() ;

            var request = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = fileKey,
                BucketName = _bucket,
                ContentType = file.ContentType,
                CannedACL = S3CannedACL.PublicRead
            };


            var transfer = new TransferUtility(_s3);
            await transfer.UploadAsync(request);


            var url = $"https://{_bucket}.s3.amazonaws.com/{fileKey}";

            return (url, fileKey);


        }


        public string GenerateSignedUrl(string fileKey)
        {
            var request = new Amazon.S3.Model.GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = fileKey,
                Expires = DateTime.UtcNow.AddMinutes(50) 
            };

            return _s3.GetPreSignedURL(request);
        }




    }
}
