using System.ComponentModel.DataAnnotations;
using MIO.Core.Enums;

namespace MIO2.Core.DTOs.GalleryDtos;

public class CreateUploadUrlDto
{
    [Required]
    public string FileName { get; set; } = null!;
    [Required]
    public string ContentType { get; set; } = null!;
    [Required]
    public long Size { get; set; }
    
    public MediaCategory Category { get; set; }
}
    
