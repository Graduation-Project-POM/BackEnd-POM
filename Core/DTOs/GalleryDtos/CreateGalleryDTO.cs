using System.ComponentModel.DataAnnotations;
using MIO.Core.Enums;

namespace MIO2.Core.DTOs.GalleryDtos
{
    public class CreateGalleryDTO
    {
        [Required]
        public string? FileKey { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }
        
        public MediaCategory Category { get; set; }
        
        [Required]
        public string RefType { get; set; } = null!;


    }
}
