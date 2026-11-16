using System.ComponentModel.DataAnnotations;

namespace MIO.Core.DTOs.ProblemDTOs;

// Instructor grades a student's submission.
public class GradeSubmissionDto
{
    // Nullable so a missing score is rejected (see ProblemService.GradeSubmission)
    // instead of silently binding to 0 — e.g. when only feedback was entered.
    [Range(0, 1000)]
    public int? Grade { get; set; }

    [MaxLength(2000)]
    public string? Feedback { get; set; }
}
