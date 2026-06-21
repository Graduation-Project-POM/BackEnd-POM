using System.ComponentModel.DataAnnotations;
using MIO.Core.Enums;

public class createContestDTO
{
    [Required]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Required]
    public ContestStatus Status { get; set; } = ContestStatus.Upcoming;
    public ICollection<int> ProblemIds { get; set; } = [];
    [Required]
    public DateTime StartAt { get; set; }
    [Required]
    public DateTime EndAt { get; set; }

    // A contest must be assigned to exactly one group on creation. Required.
    [Required]
    public int GroupId { get; set; }
}