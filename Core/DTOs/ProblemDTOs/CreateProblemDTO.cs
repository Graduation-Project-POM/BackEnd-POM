using System.ComponentModel.DataAnnotations;
using MIO.Core.Enums;

public class CreateProblemDto
{
    [Required, MaxLength(255)]
    public string Title { get; set; }

    [Required]
    public string Statement { get; set; }

    
    
    [Required]
    public Difficulty Difficulty { get; set; }

    [Required, MaxLength(255)]
    public string Topic { get; set; }

    public string Solution { get; set; }
    
    [ MaxLength(500)]
    public string? PhotoFileKey { get; set; }

    
    public int TrainerID { get; set; }

    public bool IsPublished { get; set; }
}