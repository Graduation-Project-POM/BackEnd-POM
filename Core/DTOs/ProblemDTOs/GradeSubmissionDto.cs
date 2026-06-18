using System.ComponentModel.DataAnnotations;

namespace MIO.Core.DTOs.ProblemDTOs;

public class GradeSubmissionDto
{
    [Range(0, 100)]
    public int Grade { get; set; }
}