using MIO.Core.Enums;

namespace MIO.Core.DTOs.ContestDTOs;

public class UpdateContestDTO
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public ContestStatus? Status { get; set; }
    public Difficulty? Difficulty { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public List<int>? ProblemIds { get; set; }
    // Moves the contest to this group when set.
    public int? GroupId { get; set; }
}