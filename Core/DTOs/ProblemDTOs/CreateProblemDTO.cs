using System.ComponentModel.DataAnnotations;
using MIO.Core.Enums;

public class CreateProblemDto
{
    [Required, MaxLength(255)]
    public string Title { get; set; }

    public string? Statement { get; set; }

    [Required]
    public Difficulty Difficulty { get; set; }

    [Required, MaxLength(255)]
    public string Topic { get; set; }

    public string? Solution { get; set; }
    
    [ MaxLength(500)]
    public string? PhotoFileKey { get; set; }

    
    public int TrainerID { get; set; }

    public bool IsPublished { get; set; }

    [Range(1, 1000, ErrorMessage = "Points must be between 1 and 1000.")]
    public int Points { get; set; } = 100;

    public bool IsBankVisible { get; set; } = true;
}