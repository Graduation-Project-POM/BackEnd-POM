using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIO.API.Settings;
using MIO.Core.DTOs.StudentDtos;
using MIO2.Core.DTOs.GalleryDtos;
using MIO2.Core.Interfaces;
using MIO2.Infrastructure.Services;

namespace MIO2.API.Controllers
{

    [ApiController]
    [Route("[Controller]")]
    [Authorize]
    public class GalleryController: ControllerBase
    {
        private readonly IS3Service _s3Service;
        private readonly IGalleryService _galleryService;
        public GalleryController(IS3Service s3Service,IGalleryService galleryService) 
        {
            _s3Service = s3Service;
            _galleryService = galleryService;
            
        }



        [HttpPost]
        [Authorize(Roles ="Admin")]
        [Route("upload-url")]
        public async Task<ActionResult> UploadMediaRequest(
            [FromBody] CreateUploadUrlDto UploadDto)
        {
            
            var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            var allowedVideoTypes= new[] { "video/mp4", "video/webm" };
            
            if (!allowedImageTypes.Contains(UploadDto.ContentType) &&
                !allowedVideoTypes.Contains(UploadDto.ContentType))
                return BadRequest("Invalid file type.");
        
            
            
            if (allowedImageTypes.Contains(UploadDto.ContentType) &&
                UploadDto.Size > 5 * 1024 * 1024  )
                return BadRequest("File too large.");
        
            if(allowedVideoTypes.Contains(UploadDto.ContentType) &&
               UploadDto.Size > 200 * 1024 * 1024)
                return BadRequest("File too large.");
            
            
            var (fileKey, signedUrl) =_s3Service.CreateUploadURL(UploadDto.FileName,
                UploadDto.ContentType);
            
            return Ok(new
            {
                message = "Pre-Signed Url Created Successfully",
                FileKey=fileKey,
                PreSignedUrl =signedUrl,
            });
        
        }

        
        [HttpPost]
        [Authorize(Roles ="Admin")]
        [Route("upload-media")]
        public async Task<ActionResult> UploadAMediaInDbAfterUploadingInS3([FromBody]CreateGalleryDTO galleryDTO)
        {
           
                
            if(!await _s3Service.IsMediaExists(galleryDTO.FileKey))
                return BadRequest("the file doesn't exist ");
            
            var url=_s3Service.GetPublicUrl(galleryDTO.FileKey);

         
            
            var Media=await _galleryService.UploadMediaToGallery(galleryDTO,url);

            
            
            return Ok(new
                {
                    message = "Media Uploaded Successfully",
                    MediatId = Media.Id,
                    MediaUrl = Media.Url
                }
            );


        }
        
        
        [AllowAnonymous]
        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult> GetMedia(int id)
        {
        
            var Media =await  _galleryService.GetMediaFromGallery(id);
        
            
            return Ok(new
              {
                  message = "Media Retrieved successfully",
                  MediaID = id,
                  MediaUrl = Media.Url,
                  Title=Media.Title,
                  Type=Media.Type,
                  Description=Media.Description,
              });
        
        }
      
        [HttpDelete]
        [Authorize(Roles ="Admin")]
        [Route("{id}")]
        public async Task<ActionResult> DeleteMedia(int id)
        {
            
            var Media =await  _galleryService.GetMediaFromGallery(id);

            if (await _s3Service.DeleteMediaAsync(Media.FileKey))
                await _galleryService.DeleteMediaFromGallery(id);

            return Ok(new
                    {
                        ID=Media.Id,
                    }
                    );
        }


    }
}
