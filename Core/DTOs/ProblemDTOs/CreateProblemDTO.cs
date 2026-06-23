using System.ComponentModel.DataAnnotations;
using MIO.Core.DTOs.ProblemDTOs;
using MIO.Core.Enums;

public class CreateProblemDto
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

    public string? Solution { get; set; }
    
    [ MaxLength(500)]
    public string? PhotoFileKey { get; set; }

    
    public int TrainerID { get; set; }

    [Required(ErrorMessage = "IsPublished is required. Choose Draft (false) or Publish (true).")]
    public bool? IsPublished { get; set; }

    public bool IsBankProblem { get; set; } = true;
}