using MIO.Core.Enums;

namespace MIO.Core.DTOs.ProblemDTOs;


public class MyProblemSolutionsDto
{
    public int ProblemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Statement { get; set; }
    public Difficulty Difficulty { get; set; }
    public string Topic { get; set; } = string.Empty;

    public List<BankSubmissionResponseDto> BankSubmissions { get; set; } = new();
    public List<SubmissionResponseDto> ContestSubmissions { get; set; } = new();
}
