using System.ComponentModel.DataAnnotations;
using MIO.Core.Enums;

public class UpdateProblemDto
{
    [Required, MaxLength(255)]
    public string Title { get; set; }

    public string? Statement { get; set; }

    [Required]
    public Difficulty Difficulty { get; set; }

    [Required, MaxLength(255)]
    public string Topic { get; set; }

    [Url, MaxLength(500)]
    public string? PhotoFileKey { get; set; }

    public bool IsPublished { get; set; }
}