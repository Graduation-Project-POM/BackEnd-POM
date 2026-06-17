namespace MIO.Core.DTOs.ProblemDTOs;

public class SubmitSolutionResponseDto
{
    public int SubmissionId { get; set; }
    public int ProblemId { get; set; }
    public int StudentId { get; set; }
    public string? SolutionText { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime SubmittedAt { get; set; }
}