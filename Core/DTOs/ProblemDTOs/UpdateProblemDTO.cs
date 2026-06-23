using System.ComponentModel.DataAnnotations;
using MIO.Core.DTOs.ProblemDTOs;
using MIO.Core.Enums;

public class UpdateProblemDto
{
    [Required, MaxLength(255)]
    public string Title { get; set; }

    public string? Statement { get; set; }

    public string? Constraints { get; set; }

    public List<ProblemExampleDto>? Examples { get; set; }

    [Required]
    public Difficulty Difficulty { get; set; }

    [Required, MaxLength(255)]
    public string Topic { get; set; }

    [MaxLength(500)]
    public string? PhotoFileKey { get; set; }

    [Range(1, 1000, ErrorMessage = "Points must be between 1 and 1000.")]
    public int Points { get; set; } = 100;
    [Required(ErrorMessage = "IsPublished is required. Choose Draft (false) or Publish (true).")]
    public bool? IsPublished { get; set; }
}
