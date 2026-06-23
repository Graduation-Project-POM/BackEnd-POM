using MIO.Core.Entities;

namespace MIO2.Core.Entities;

public class ProblemBankSubmission
{
    public int Id { get; set; }

    public int ProblemId { get; set; }
    public Problem Problem { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; }

    public string? SolutionText { get; set; }
    public string? SolutionPhotoKey { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
