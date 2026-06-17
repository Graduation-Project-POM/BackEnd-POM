using MIO.Core.Entities;
using MIO.Core.Enums;

namespace MIO2.Core.Entities;

public class Contest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public int? TrainerId { get; set; }
    public Trainer? trainer { get; set; }
    
    
    public ContestStatus Status { get; set; } = ContestStatus.Upcoming;
    
    
    public ICollection<ContestStudent> ContestStudents { get; set; } = [];
    public ICollection<Problem> Problems { get; set; } = new List<Problem>();
    
    
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    
    
}