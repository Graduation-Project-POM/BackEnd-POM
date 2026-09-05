using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIO.Core.DTOs.ProblemDTOs;
using MIO2.Core.Interfaces;

[ApiController]
[Route("api/Problems")]
[Authorize]
public class ProblemDiscussionController : ControllerBase
{
    private readonly IProblemDiscussionService _discussion;

    public ProblemDiscussionController(IProblemDiscussionService discussion)
    {
        _discussion = discussion;
    }

    [HttpGet("{problemId:int}/discussion")]
    [Authorize(Roles = "Admin,Trainer,Student")]
    public async Task<ActionResult<IEnumerable<ProblemCommentDto>>> GetDiscussion(int problemId)
    {
        if (problemId <= 0)
            return BadRequest("Invalid ID");

        return Ok(await _discussion.GetDiscussion(problemId));
    }

    [HttpPost("{problemId:int}/discussion")]
    [Authorize(Roles = "Admin,Trainer,Student")]
    public async Task<ActionResult<ProblemCommentDto>> AddComment(
        int problemId, [FromBody] CreateProblemCommentDto dto)
    {
        if (problemId <= 0)
            return BadRequest("Invalid ID");

        return Ok(await _discussion.AddComment(problemId, dto));
    }

    [HttpPut("discussion/{commentId:int}")]
    [Authorize(Roles = "Admin,Trainer,Student")]
    public async Task<ActionResult<ProblemCommentDto>> UpdateComment(
        int commentId, [FromBody] UpdateProblemCommentDto dto)
    {
        if (commentId <= 0)
            return BadRequest("Invalid ID");

        return Ok(await _discussion.UpdateComment(commentId, dto));
    }

    [HttpDelete("discussion/{commentId:int}")]
    [Authorize(Roles = "Admin,Trainer,Student")]
    public async Task<ActionResult> DeleteComment(int commentId)
    {
        if (commentId <= 0)
            return BadRequest("Invalid ID");

        await _discussion.DeleteComment(commentId);
        return NoContent();
    }
}
