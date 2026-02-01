namespace MIO2.Core.Interfaces
{
    public interface IS3Service
    {

        Task<(string url, string fileKey)> UploadPicture(IFormFile file);

        string GenerateSignedUrl(string fileKey);
    }
}
