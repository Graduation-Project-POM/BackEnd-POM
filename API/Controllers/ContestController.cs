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
    public async Task<IActionResult> AddStudents(int contestId, [FromBody] AddStudentsToContestDTO dto)
    {
        var result = await _contestService.AddStudentToContest(contestId, dto);
        return Ok(result);
    }
    
    
    
}