using MIO.Core.Enums;

namespace MIO.Core.DTOs.ProblemDTOs;

public class SubmissionResponseDto
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; }
    public string? StudentEmail { get; set; }
    public string? SolutionText { get; set; }
    public string? PhotoUrl { get; set; }
    public int? Grade { get; set; }
    public string? Feedback { get; set; }
    public DateTime? GradedAt { get; set; }
    public DateTime SubmittedAt { get; set; }
    public bool IsMine { get; set; }

    public string? ProblemTitle { get; set; }
    public string? ProblemTopic { get; set; }
    public string? ProblemStatement { get; set; }
    public Difficulty ProblemDifficulty { get; set; }

    public int ProblemPoints { get; set; }
}
