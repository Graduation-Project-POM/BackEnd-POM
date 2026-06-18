using MIO2.Core.Entities;

namespace MIO.Core.Entities;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    public int? TrainerId { get; set; }
    public Trainer? Trainer { get; set; }

    public ICollection<GroupStudent> GroupStudents { get; set; } = [];
    public ICollection<GroupContest> GroupContests { get; set; } = [];
}