using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIO.Core.DTOs.ProblemDTOs;
using MIO2.Core.Interfaces;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class ProblemsController : ControllerBase
{
    private readonly IProblemServices _Problemservice;
    //private readonly IS3Service _s3Service;

    public ProblemsController(IProblemServices problemservice)
    {
        _Problemservice = problemservice;
       
    }
    // public ProblemsController(IProblemServices problemservice,IS3Service s3Service)
    // {
    //     _Problemservice = problemservice;
    //     _s3Service = s3Service;
    // }
    [HttpGet]
    [Authorize(Roles ="Admin,Trainer")]
    public async Task<ActionResult<IEnumerable<ProblemResponseDTO>>> GetAll()
    {
        return Ok(await _Problemservice.GetAll());
    }

    [HttpGet("{id}")]
    [Authorize(Roles ="Admin,Trainer")]
    public async Task<ActionResult<ProblemResponseDTO>> Get(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid ID");
        
        var result = await _Problemservice.GetById(id);
        return Ok(result);
    }

    // [HttpPost("upload-url")]            
    // [Authorize(Roles = "Admin,Trainer")]
    // public ActionResult GetUploadUrl([FromQuery] string fileName, [FromQuery] string contentType)
    // {
    //     var (fileKey, preSignedUrl) = _s3Service.CreateUploadURL(fileName, contentType);
    //     
    //     return Ok(new { fileKey, preSignedUrl });
    // }
    
    [HttpPost]
    [Authorize(Roles ="Admin,Trainer")]
    public async Task<ActionResult> Create([FromBody] CreateProblemDto dto)
    {
        var id = await _Problemservice.Create(dto);
        return CreatedAtAction("Get", new { id }, new { problemId = id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles ="Admin,Trainer")]
    public async Task<ActionResult> Update(int id, UpdateProblemDto dto)
    {
        if (id <= 0)
            return BadRequest("Invalid ID");
        
        var success = await _Problemservice.Update(id, dto);
       

        return Ok(new
        {
            message = "Problem updated successfully",
            ProblemId = id
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles ="Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid ID");
        
        var success = await _Problemservice.Delete(id);
      

        return Ok(new
        {
            message = "Problem deleted successfully",
            ProblemId = id
        });
    }
}