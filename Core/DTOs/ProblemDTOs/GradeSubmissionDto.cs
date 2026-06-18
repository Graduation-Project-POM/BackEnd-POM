using System.ComponentModel.DataAnnotations;

namespace MIO.Core.DTOs.ProblemDTOs;

// Instructor grades a student's submission.
public class GradeSubmissionDto
{
    [Range(0, 100)]
    public int Grade { get; set; }

    [MaxLength(2000)]
    public string? Feedback { get; set; }
}
