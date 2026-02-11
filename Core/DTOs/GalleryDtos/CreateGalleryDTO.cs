using System.ComponentModel.DataAnnotations;

namespace MIO2.Core.DTOs.GalleryDtos
{
    public class CreateGalleryDTO
    {
        [Required]
        public string? FileKey { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }
        [Required]
        public string RefType { get; set; } = null!;


    }
}
