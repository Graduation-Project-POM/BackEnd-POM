using System.ComponentModel.DataAnnotations;

namespace MIO2.Core.DTOs.GalleryDtos;

public class CreateUploadUrlDto
{
    [Required]
    public string FileName { get; set; } = null!;
    [Required]
    public string ContentType { get; set; } = null!;
    [Required]
    public long Size { get; set; }
}