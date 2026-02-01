using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [Route("")]
        public async Task<ActionResult> UploadAImage(
            [FromForm] IFormFile file,
            [FromForm] CreateGalleryDTO galleryDTO)
        {

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");


            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };

            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest("Invalid file type.");

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("File too large.");


            var (url, fileKey) = await _s3Service.UploadPicture(file);


           var Image=await  _galleryService.UploadPicToGallery(galleryDTO,url,fileKey);

            //var signedUrl= _s3Service.GenerateSignedUrl(fileKey); 
           

            return Ok(new
            {
                message = "Image uploaded successfully",
                ImageID=Image.Id,
                ImageUrl =url,
            });

        }

        [AllowAnonymous]
        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult> GetImage(int id)
        {

            var imageUrl =await  _galleryService.GetPicFromGallery(id);

            
            return Ok(new
              {
                  message = "Image Retrieved successfully",
                  ImageID = id,
                  ImageUrl = imageUrl,
              });

        }


    }
}
