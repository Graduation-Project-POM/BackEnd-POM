using MIO.Core.Enums;

namespace MIO.Core.DTOs.ContestDTOs;

public class ContestDTO
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public ContestStatus Status { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TrainerId { get; set; }
    public int ProblemCount { get; set; }
    public int StudentCount { get; set; }
}