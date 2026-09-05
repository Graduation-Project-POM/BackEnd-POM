using MIO.Core.Entities;

namespace MIO2.Core.Entities;

public class ProblemComment
{
    public int Id { get; set; }

    public int ProblemId { get; set; }
    public Problem Problem { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? ParentCommentId { get; set; }
    public ProblemComment? ParentComment { get; set; }
    public ICollection<ProblemComment> Replies { get; set; } = [];

    public string? Body { get; set; }

    public string? PhotoFileKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
