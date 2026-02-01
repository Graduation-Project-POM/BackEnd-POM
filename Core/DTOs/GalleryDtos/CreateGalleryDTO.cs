using System.ComponentModel.DataAnnotations;

namespace MIO2.Core.DTOs.GalleryDtos
{
    public class CreateGalleryDTO
    {
        
     

        public string? Title { get; set; }
        [Required]
        public string Type { get; set; }
        public string? Description { get; set; }
        [Required]
        public string RefType { get; set; }


    }
}
