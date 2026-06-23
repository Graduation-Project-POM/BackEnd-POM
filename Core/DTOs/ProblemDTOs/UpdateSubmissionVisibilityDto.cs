using System.ComponentModel.DataAnnotations;

namespace MIO.Core.DTOs.ProblemDTOs;


public class UpdateSubmissionVisibilityDto
{
    [Required]
    public bool? IsPublished { get; set; }
}
