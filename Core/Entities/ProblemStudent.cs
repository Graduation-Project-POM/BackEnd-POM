using MIO.Core.Entities;

namespace MIO2.Core.Entities;

public class ProblemStudent
{
    public int Id { get; set; }

    public int ProblemId { get; set; }
    public Problem Problem { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; }

    public string? SubmittedSolutionText { get; set; }
    public string? SubmittedSolutionPhotoKey { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public int? Score { get; set; }
    public string? Feedback { get; set; }
    public DateTime? GradedAt { get; set; }

    public bool IsPublished { get; set; } = true;
}