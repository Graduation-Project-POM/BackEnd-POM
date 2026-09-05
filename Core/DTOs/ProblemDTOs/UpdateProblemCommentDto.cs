using System.ComponentModel.DataAnnotations;

namespace MIO.Core.DTOs.ProblemDTOs;

public class UpdateProblemCommentDto
{
    [MaxLength(4000)]
    public string? Body { get; set; }

    public string? PhotoFileKey { get; set; }
}
