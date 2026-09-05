using System.ComponentModel.DataAnnotations;

namespace MIO.Core.DTOs.ProblemDTOs;

public class CreateProblemCommentDto
{
    [MaxLength(4000)]
    public string? Body { get; set; }

    public string? PhotoFileKey { get; set; }

    public int? ParentCommentId { get; set; }
}
