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
using MIO.Core.Enums;

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


       

        public async Task<Gallery> UploadMediaToGallery(CreateGalleryDTO galleryDTO)
        {
            char type = galleryDTO.Category switch
            {
                MediaCategory.GalleryVideo => 'V',

                MediaCategory.UserProfile => 'I',
                MediaCategory.QuestionImage => 'I',
                MediaCategory.SolutionImage => 'I',
                MediaCategory.GalleryImage => 'I',
                MediaCategory.AnnouncementImage => 'I',

                _ => throw new AppException(
                    "Invalid media category",
                    StatusCodes.Status400BadRequest)
            };
            

            var Media = new Gallery
            {
            FileKey=galleryDTO.FileKey,
            Title= galleryDTO.Title,
            Type= type,
            Category = galleryDTO.Category,
            Description= galleryDTO.Description,
            UploadedByID= _currentUser.UserId,
            RefType=galleryDTO.RefType,
            RefID=1        // will be changed after adding an event and News table
            };




            _dbContext.Gallery.Add(Media);
            await _dbContext.SaveChangesAsync();
            return Media;


        }


        public async Task<Gallery> GetMediaFromGallery(int MediaId)
        {

            var Media=await _dbContext.Gallery.FindAsync(MediaId);

            if (Media == null || Media.IsDeleted)
                throw new AppException(
                        $"Media with ID {MediaId} not found",
                        StatusCodes.Status404NotFound);


            return Media;


        }

        public async Task<Gallery> DeleteMediaFromGallery(int MediaId)
        {
            
            var Media=await _dbContext.Gallery.FindAsync(MediaId);

            if (Media == null)
                throw new AppException(
                    $"Media with ID {MediaId} not found",
                    StatusCodes.Status404NotFound);
            
            Media.IsDeleted = true;
            
            _dbContext.Set<Gallery>().Remove(Media);

            await _dbContext.SaveChangesAsync();

            return Media;
            
            
            
        }
       
    }
}
