using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MIO.Core.DTOs.ProblemDTOs;
using MIO2.Core.Interfaces;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProblemsController : ControllerBase
{
    private readonly IProblemServices _Problemservice;
    private readonly IS3Service _s3Service;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ProblemsController> _logger;

    public ProblemsController(IProblemServices problemservice, IS3Service s3Service,
                              IWebHostEnvironment env, ILogger<ProblemsController> logger)
    {
        _Problemservice = problemservice;
        _s3Service = s3Service;
        _env = env;
        _logger = logger;
    }


    [HttpPost("upload-image")]
    [Authorize(Roles = "Admin,Trainer,Student")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> UploadImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        const long maxBytes = 5 * 1024 * 1024; // 5 MB
        if (file.Length > maxBytes)
            return BadRequest(new { message = "Image must be 5 MB or smaller." });

        var allowed = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(new { message = "Unsupported image type. Use PNG, JPG, GIF or WEBP." });

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Unsupported image type. Use PNG, JPG, GIF or WEBP." });

        var key = $"uploads/{Guid.NewGuid():N}{ext}";

        if (_s3Service.IsConfigured)
        {
            try
            {
                await using var s3Stream = file.OpenReadStream();
                var s3Url = await _s3Service.UploadPublicAsync(s3Stream, key, contentType);
                return Ok(new { url = s3Url });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "S3 upload failed for {Key}; falling back to local storage.", key);
            }
        }

        // Local fallback: wwwroot/uploads is served as static files by Program.cs.
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var localPath = Path.Combine(webRoot, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await using (var fs = System.IO.File.Create(localPath))
        {
            await file.CopyToAsync(fs);
        }

        return Ok(new { url = "/" + key });
    }
    // public ProblemsController(IProblemServices problemservice,IS3Service s3Service)
    // {
    //     _Problemservice = problemservice;
    //     _s3Service = s3Service;
    // }
    [HttpGet]
    [Authorize(Roles ="Admin,Trainer,Student")]
    public async Task<ActionResult<IEnumerable<ProblemResponseDTO>>> GetAll()
    {
        return Ok(await _Problemservice.GetAll());
    }

    [HttpGet("{id}")]
    [Authorize(Roles ="Admin,Trainer,Student")]
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
    [Authorize(Roles ="Admin,Trainer")]
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
    
    [HttpPost("{id}/submit")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult> SubmitSolution(int id, SubmitSolutionDto dto)
    {
        var result = await _Problemservice.SubmitSolution(id, dto);
        return Ok(result);
    }

    [HttpGet("{id}/submissions")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult<IEnumerable<SubmissionResponseDto>>> GetSubmissions(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid ID");

        return Ok(await _Problemservice.GetSubmissions(id));
    }

    [HttpGet("my/submissions")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<IEnumerable<SubmissionResponseDto>>> GetMySubmissions()
    {
        return Ok(await _Problemservice.GetMySubmissions());
    }

    [HttpPut("submissions/{submissionId}/grade")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult> GradeSubmission(int submissionId, GradeSubmissionDto dto)
    {
        if (submissionId <= 0)
            return BadRequest("Invalid ID");

        var result = await _Problemservice.GradeSubmission(submissionId, dto);
        return Ok(result);
    }


    [HttpPost("{id}/bank-submit")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult> SubmitBankSolution(int id, SubmitSolutionDto dto)
    {
        if (id <= 0)
            return BadRequest("Invalid ID");

        var result = await _Problemservice.SubmitBankSolution(id, dto);
        return Ok(result);
    }

    [HttpGet("{id}/bank-submissions")]
    [Authorize(Roles = "Admin,Trainer,Student")]
    public async Task<ActionResult<IEnumerable<BankSubmissionResponseDto>>> GetBankSubmissions(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid ID");

        return Ok(await _Problemservice.GetBankSubmissions(id));
    }

    [HttpGet("bank/my-submissions")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<IEnumerable<BankSubmissionResponseDto>>> GetMyBankSubmissions()
    {
        return Ok(await _Problemservice.GetMyBankSubmissions());
    }

    [HttpGet("{id}/bank/my-submissions")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<IEnumerable<BankSubmissionResponseDto>>> GetMyBankSubmissionsForProblem(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid ID");

        return Ok(await _Problemservice.GetMyBankSubmissionsForProblem(id));
    }

    [HttpGet("my-solutions")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<IEnumerable<MySolvedProblemSummaryDto>>> GetMySolvedProblems()
    {
        return Ok(await _Problemservice.GetMySolvedProblems());
    }

    [HttpGet("{id}/my-solutions")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<MyProblemSolutionsDto>> GetMyProblemSolutions(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid ID");

        var result = await _Problemservice.GetMyProblemSolutions(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{id}/all-solutions")]
    [Authorize(Roles = "Admin,Trainer,Student")]
    public async Task<ActionResult<MyProblemSolutionsDto>> GetAllProblemSolutions(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid ID");

        var result = await _Problemservice.GetAllProblemSolutions(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // Student Problem Bank: own submissions always; other students' published
    // submissions only when includeOthers=true.
    [HttpGet("{id}/bank/solutions")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<StudentProblemSolutionsDto>> GetStudentProblemSolutions(int id, [FromQuery] bool includeOthers = false)
    {
        if (id <= 0)
            return BadRequest("Invalid ID");

        return Ok(await _Problemservice.GetStudentProblemSolutions(id, includeOthers));
    }

    [HttpGet("{id}/my-drafts")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<MyProblemSolutionsDto>> GetMyDraftsForProblem(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid ID");

        var result = await _Problemservice.GetMyDraftsForProblem(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("bank-submissions/{submissionId}")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult> DeleteBankSubmission(int submissionId)
    {
        if (submissionId <= 0) return BadRequest("Invalid ID");
        await _Problemservice.DeleteBankSubmission(submissionId);
        return NoContent();
    }

    [HttpPatch("bank-submissions/{submissionId}/visibility")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult> SetBankSubmissionVisibility(int submissionId, [FromBody] UpdateSubmissionVisibilityDto dto)
    {
        if (submissionId <= 0) return BadRequest("Invalid ID");
        await _Problemservice.SetBankSubmissionVisibility(submissionId, dto.IsPublished!.Value);
        return NoContent();
    }

    [HttpDelete("submissions/{submissionId}")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult> DeleteContestSubmission(int submissionId)
    {
        if (submissionId <= 0) return BadRequest("Invalid ID");
        await _Problemservice.DeleteContestSubmission(submissionId);
        return NoContent();
    }

    [HttpPatch("submissions/{submissionId}/visibility")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult> SetContestSubmissionVisibility(int submissionId, [FromBody] UpdateSubmissionVisibilityDto dto)
    {
        if (submissionId <= 0) return BadRequest("Invalid ID");
        await _Problemservice.SetContestSubmissionVisibility(submissionId, dto.IsPublished!.Value);
        return NoContent();
    }

}