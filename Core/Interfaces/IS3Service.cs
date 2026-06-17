using MIO.Core.Enums;

namespace MIO2.Core.Interfaces
{
    public interface IS3Service
    {

        

        string GenerateSignedUrl(string fileKey);
        (string fileKey,string preSignedUrl) CreateUploadURL(string fileName, string contentType,MediaCategory category, long size);
        Task<bool> IsMediaExists(string key);
        string GetPublicUrl(string key);
        Task<bool> DeleteMediaAsync(string key);
    }
}
