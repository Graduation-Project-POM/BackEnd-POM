using MIO.Core.Enums;

namespace MIO.Core.DTOs.ProblemDTOs;

public class ProblemResponseDTO
{
    public int ID { get; set; }
    public string Title { get; set; }
    public string Statement { get; set; }
    public Difficulty Difficulty { get; set; }
    public string Topic { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsPublished { get; set; }

    public string? PublisherName { get; set; }
    public bool CanModify { get; set; }
}