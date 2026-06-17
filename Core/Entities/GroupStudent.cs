using MIO.Core.Entities;

namespace MIO2.Core.Entities;

public class GroupStudent
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; }

    public DateOnly JoinDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
}