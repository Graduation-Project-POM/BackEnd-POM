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
            var (fileKey, signedUrl) =_s3Service.CreateUploadURL(UploadDto.FileName,
                UploadDto.ContentType,UploadDto.Category,UploadDto.Size);
            
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
            
            var Media=await _galleryService.UploadMediaToGallery(galleryDTO);
            
            return Ok(new
                {
                    message = "Media Uploaded Successfully",
                    MediatId = Media.Id,
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
                  MediaUrl = _s3Service.GenerateSignedUrl(Media.FileKey),
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
