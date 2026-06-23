using MIO.Core.Enums;

namespace MIO.Core.DTOs.ProblemDTOs;

// A contest problem together with the current student's submission + grade
// (if any). Powers the student's contest view: solve while running, read-only
// results/marks after it ends.
public class StudentContestProblemDto
{
    public int ProblemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Statement { get; set; }
    public Difficulty Difficulty { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }

    public bool Submitted { get; set; }
    public int? SubmissionId { get; set; }
    public string? SolutionText { get; set; }
    public string? SolutionPhotoUrl { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public int? Score { get; set; }
    public string? Feedback { get; set; }
    public DateTime? GradedAt { get; set; }

    public int Points { get; set; }
    public int SolvedCount { get; set; }
}
