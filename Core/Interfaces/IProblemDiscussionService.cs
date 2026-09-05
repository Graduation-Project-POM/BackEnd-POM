using MIO.Core.DTOs.ProblemDTOs;

namespace MIO2.Core.Interfaces;

public interface IProblemDiscussionService
{
    Task<IEnumerable<ProblemCommentDto>> GetDiscussion(int problemId);
    Task<ProblemCommentDto> AddComment(int problemId, CreateProblemCommentDto dto);
    Task<ProblemCommentDto> UpdateComment(int commentId, UpdateProblemCommentDto dto);
    Task DeleteComment(int commentId);
}
