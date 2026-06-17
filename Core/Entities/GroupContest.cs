using MIO.Core.Entities;
using MIO2.Core.Entities;

namespace MIO.Core.Entities;

public class GroupContest
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; }

    public int ContestId { get; set; }
    public Contest Contest { get; set; }

    public DateOnly AddDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
}