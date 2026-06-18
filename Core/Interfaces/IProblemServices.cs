using MIO.Core.DTOs.ProblemDTOs;

namespace MIO2.Core.Interfaces;

public interface IProblemServices
{
    Task<IEnumerable<ProblemResponseDTO>> GetAll();
    Task<ProblemResponseDTO> GetById(int id);
    Task<int> Create(CreateProblemDto dto);
    Task<bool> Update(int id, UpdateProblemDto dto);
    Task<bool> Delete(int id);
    Task<SubmitSolutionResponseDto> SubmitSolution(int problemId, SubmitSolutionDto dto);

    // Grading
    Task<IEnumerable<SubmissionDto>> GetSubmissionsForProblem(int problemId);
    Task<SubmissionDto> GradeSubmission(int submissionId, GradeSubmissionDto dto);
}