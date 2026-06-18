using System.ComponentModel.DataAnnotations;
using MIO.Core.Enums;

public class UpdateProblemDto
{
    [Required, MaxLength(255)]
    public string Title { get; set; }

    public string? Statement { get; set; }

    // No [MaxLength]: Difficulty is an enum; MaxLength only applies to
    // strings/arrays and throws at validation time otherwise.
    [Required]
    public Difficulty Difficulty { get; set; }

    [Required, MaxLength(255)]
    public string Topic { get; set; }

    //[Url, MaxLength(500)]
    // No [Url]: images are stored as relative paths (e.g. "/uploads/x.png"),
    // which [Url] would reject. Nullable so an empty value clears the image.
    [MaxLength(500)]
    public string? PhotoFileKey { get; set; }

    public bool IsPublished { get; set; }
}