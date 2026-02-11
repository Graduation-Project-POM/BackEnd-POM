using MIO2.Core.DTOs.GalleryDtos;
using MIO2.Core.Entities;

namespace MIO2.Core.Interfaces
{
    public interface IGalleryService
    {

        Task<Gallery> UploadMediaToGallery(CreateGalleryDTO galleryDTO,string url);

        Task<Gallery> GetMediaFromGallery(int MediaId);

        Task<Gallery> DeleteMediaFromGallery(int MediaId);
    }
}
