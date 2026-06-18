namespace MIO.Core.DTOs.ProblemDTOs;

// A student's submission to a problem, for the instructor's grading view.
public class SubmissionDto
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? SolutionText { get; set; }
    public string? SolutionPhotoUrl { get; set; }
    public DateTime SubmittedAt { get; set; }

    public int? Score { get; set; }
    public string? Feedback { get; set; }
    public DateTime? GradedAt { get; set; }
}
