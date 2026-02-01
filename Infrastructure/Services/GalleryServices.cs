using Amazon.S3.Model;
using Amazon.S3.Transfer;
using MIO.API.Settings;
using MIO.Core.Entities;
using MIO.Core.Interfaces;
using MIO.Infrastructure;
using MIO.Infrastructure.Services;
using MIO2.Core.DTOs.GalleryDtos;
using MIO2.Core.Entities;
using MIO2.Core.Interfaces;
using System.Globalization;
namespace MIO2.Infrastructure.Services
{
    public class GalleryServices: IGalleryService
    {
        private readonly AppDbContext _dbContext;
        private readonly ICurrentUserService _currentUser;
        private readonly IS3Service _s3Service;
        public GalleryServices(AppDbContext dbContext, ICurrentUserService currentUser, IS3Service s3Service)
        { 
            _dbContext=dbContext;        
            _currentUser=currentUser;
            _s3Service=s3Service;
        }


       

        public async Task<Gallery> UploadPicToGallery(CreateGalleryDTO galleryDTO, string url,
            string fileKey)
        {
          

            var Image = new Gallery
            {
            Url= url,
            FileKey=fileKey,
            Title= galleryDTO.Title,
            Type= galleryDTO.Type,
            Description= galleryDTO.Description,
            UploadedByID= _currentUser.UserId,
            RefType=galleryDTO.RefType,
            RefID=1        // will be changed after adding an event and News table
            };




            _dbContext.Gallery.Add(Image);
            await _dbContext.SaveChangesAsync();
            return Image;


        }


        public async Task<string> GetPicFromGallery(int ImageId)
        {

            var Image=await _dbContext.Gallery.FindAsync(ImageId);

            if (Image == null)
                throw new AppException(
                        $"Image with ID {ImageId} not found",
                        StatusCodes.Status404NotFound);


            return Image.Url;


        }


       
    }
}
