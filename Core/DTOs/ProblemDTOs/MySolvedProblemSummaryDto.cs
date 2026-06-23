using MIO.Core.Enums;

namespace MIO.Core.DTOs.ProblemDTOs;

public class MySolvedProblemSummaryDto
{
    public int ProblemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }

    public bool HasBankSubmission { get; set; }
    public bool HasContestSubmission { get; set; }

    public int? LatestContestScore { get; set; }

    public DateTime LatestSubmittedAt { get; set; }
}
