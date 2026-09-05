namespace MIO.Core.DTOs.ProblemDTOs;

public class ProblemCommentDto
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public int? ParentCommentId { get; set; }

    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public string? AuthorImageUrl { get; set; }

    public string? Body { get; set; }
    public string? PhotoUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsMine { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public List<ProblemCommentDto> Replies { get; set; } = [];
}
