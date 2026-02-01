using MIO2.Core.DTOs.GalleryDtos;
using MIO2.Core.Entities;

namespace MIO2.Core.Interfaces
{
    public interface IGalleryService
    {

        Task<Gallery> UploadPicToGallery(CreateGalleryDTO galleryDTO,string url,string fileKey);

        Task<string> GetPicFromGallery(int ImageId);
    }
}
