namespace MIO.Core.DTOs.ProblemDTOs;

/// <summary>Bank and contest submissions for one problem.</summary>
public class SubmissionListsDto
{
    public List<BankSubmissionResponseDto> BankSubmissions { get; set; } = new();
    public List<SubmissionResponseDto> ContestSubmissions { get; set; } = new();
}

/// <summary>
/// Student Problem Bank view of a problem's solutions.
/// <see cref="Mine"/> holds every submission of the current student (published
/// and hidden). <see cref="Others"/> holds other students' published
/// submissions only, and is null unless the caller explicitly asked for them.
/// </summary>
public class StudentProblemSolutionsDto
{
    public int ProblemId { get; set; }
    public SubmissionListsDto Mine { get; set; } = new();
    public SubmissionListsDto? Others { get; set; }
}
