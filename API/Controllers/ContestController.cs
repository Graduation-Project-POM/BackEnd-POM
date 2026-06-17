using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIO.Core.DTOs.ContestDTOs;
using MIO2.Core.Entities;
using MIO2.Core.Interfaces;

namespace MIO2.API.Controllers;

[ApiController]
[Route("[Controller]")]
[Authorize]
public class ContestController  : ControllerBase
{
private readonly IContestService _contestService;
    
public ContestController(IContestService contestService) 
{
    _contestService = contestService;
}

    [HttpPost]
    [Authorize(Roles = "Trainer")]
    public async Task<ActionResult<Contest>> CreateContest(createContestDTO contestDto)
    {
        var Contest = await _contestService.CreateContest(contestDto);
        
        return new OkObjectResult(new
        {
            message = "Contest Created successfully",
            ContestID = Contest.Id
        });
        
    }
    
    [HttpPost("{contestId}/students")]
    [Authorize(Policy = "ContestOwnerOrAdmin")]
    public async Task<IActionResult> AddStudents(int contestId, [FromBody] AddStudentsToContestDTO dto)
    {
        var result = await _contestService.AddStudentToContest(contestId, dto);
        return Ok(result);
    }
    
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ContestDTO>>> GetContests()
    {
        var contests = await _contestService.GetContests();
        return Ok(contests);
    }

    [HttpGet("{contestId}")]
    [Authorize(Policy = "ContestOwnerOrAdmin")]
    public async Task<ActionResult<ContestDTO>> GetContestById(int contestId)
    {
        var contest = await _contestService.GetContestById(contestId);
        return Ok(contest);
    }

    [HttpGet("{contestId}/students")]
    [Authorize(Policy = "ContestOwnerOrAdmin")]
    public async Task<ActionResult<ContestsStudentsResponseDTO>> GetContestsStudents(int contestId)
    {
        var students= await _contestService.GetContestsStudents(contestId);
        return Ok(students);
    }
    
    [HttpPut("{contestId}")]
    [Authorize(Policy = "ContestOwnerOrAdmin")]
    public async Task<IActionResult> UpdateContest(int contestId, UpdateContestDTO dto)
    {
        var result = await _contestService.UpdateContest(contestId, dto);
        return Ok(result);
    }
    
    [HttpDelete("{contestId}")]
    [Authorize(Policy = "ContestOwnerOrAdmin")]
    public async Task<IActionResult> DeleteContest(int contestId)
    {
        await _contestService.DeleteContest(contestId);
        return NoContent(); // 204
    }
    
}