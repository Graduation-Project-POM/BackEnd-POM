using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MIO.API.Settings;
using MIO.Core.DTOs.ProblemDTOs;
using MIO.Core.Entities;
using MIO.Core.Interfaces;
using MIO.Infrastructure;
using MIO2.Core.Entities;
using MIO2.Core.Interfaces;

public class ProblemService : IProblemServices
{
    private readonly AppDbContext _context;
   // private readonly IS3Service _s3Service;
    private readonly ICurrentUserService _currentUser;
    
    // public ProblemService(AppDbContext context, IS3Service s3Service, ICurrentUserService currentUser)
    // {
    //     _context = context;
    //     _s3Service = s3Service;
    //     _currentUser = currentUser;
    // }
    
    public ProblemService(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<ProblemResponseDTO>> GetAll()
    {
       

        return await _context.Problems.Select(p => new ProblemResponseDTO
        {
            ID = p.ID,
            Title = p.Title,
            Difficulty = p.Difficulty,
            //PhotoUrl = string.IsNullOrEmpty(p.PhotoFileKey) ? null : _s3Service.GetPublicUrl(p.PhotoFileKey),
            Topic = p.Topic,
            IsPublished = p.IsPublished
            
        })   .ToListAsync();;
    }

    public async Task<ProblemResponseDTO> GetById(int id)
    {
        var p = await _context.Problems.FindAsync(id);

        if (p == null)
        {
            throw new AppException(
                $"Problem with ID {id} not found",
                StatusCodes.Status404NotFound);
        }

        return new ProblemResponseDTO
        {
            ID = p.ID,
            Title = p.Title,
            Statement = p.Statement,
            Difficulty = p.Difficulty,
            Topic = p.Topic,
            IsPublished = p.IsPublished,
            // S3 URL building is disabled; expose the stored key/URL as-is so the
            // client can render it when a full URL was saved.
            PhotoUrl = string.IsNullOrEmpty(p.PhotoFileKey) ? null : p.PhotoFileKey,
        };
    }

    public async Task<int> Create(CreateProblemDto dto)
    {
       var userID=_currentUser.UserId;
       var isAdmin = _currentUser.Role == "Admin";
       var trainer=await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == userID);

        if (trainer==null && !isAdmin)
        {
            throw new AppException(
                "Invalid TrainerID. Trainer does not exist.",
                StatusCodes.Status404NotFound);
        }

      
        if (await _context.Problems.AnyAsync(p => p.Title == dto.Title))
        {
            throw new AppException(
                "Problem with same title already exists",
                StatusCodes.Status409Conflict);
        }

        var problem = new Problem
        {
            Title = dto.Title,
            Statement = dto.Statement,
            Difficulty = dto.Difficulty,
            Topic = dto.Topic,
            PhotoFileKey = dto.PhotoFileKey,
            TrainerID = trainer?.Id,
            IsPublished = dto.IsPublished,
            CreatedAt = DateTime.UtcNow
        };

        _context.Problems.Add(problem);
        await _context.SaveChangesAsync();

        return problem.ID;
    }

    public async Task<bool> Update(int id, UpdateProblemDto dto)
    {
        var problem = await _context.Problems.FindAsync(id);

        if (problem == null)
        {
            throw new AppException(
                $"Problem with ID {id} not found",
                StatusCodes.Status404NotFound);
        }

        problem.PhotoFileKey = string.IsNullOrWhiteSpace(dto.PhotoFileKey)
            ? null
            : dto.PhotoFileKey;

        problem.Title = dto.Title;
        problem.Statement = dto.Statement;
        problem.Difficulty = dto.Difficulty;
        problem.Topic = dto.Topic;
        problem.IsPublished = dto.IsPublished;
        problem.UpdatedAt = DateTime.UtcNow;

        
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var problem = await _context.Problems.FindAsync(id);

        if (problem == null)
        {
            throw new AppException(
                $"Problem with ID {id} not found",
                StatusCodes.Status404NotFound);
        }

        
        _context.Problems.Remove(problem);
        await _context.SaveChangesAsync();

        return true;
    }
    
    
    
    public async Task<SubmitSolutionResponseDto> SubmitSolution(int problemId, SubmitSolutionDto dto)
{
    
    var problem = await _context.Problems.FindAsync(problemId);
    if (problem == null)
        throw new AppException($"Problem with ID {problemId} not found", StatusCodes.Status404NotFound);

    var now = DateTime.UtcNow;
    var windows = await _context.Contests
        .Where(c => c.Problems.Any(p => p.ID == problemId))
        .Select(c => new { c.StartAt, c.EndAt })
        .ToListAsync();

    if (windows.Count > 0 && !windows.Any(w => now >= w.StartAt && now <= w.EndAt))
    {
        bool notStarted = windows.All(w => now < w.StartAt);
        throw new AppException(
            notStarted
                ? "This contest hasn't started yet."
                : "This contest has ended; submissions are closed.",
            StatusCodes.Status403Forbidden);
    }

    var userId = _currentUser.UserId;
    var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
    if (student == null)
        throw new AppException("Student not found for current user", StatusCodes.Status404NotFound);

   
    if (string.IsNullOrWhiteSpace(dto.SolutionText) && string.IsNullOrWhiteSpace(dto.PhotoFileKey))
        throw new AppException("You must provide a solution text or a photo", StatusCodes.Status400BadRequest);

    var submission = await _context.ProblemStudents
        .FirstOrDefaultAsync(ps => ps.ProblemId == problemId && ps.StudentId == student.Id);

    if (submission == null)
    {
        submission = new ProblemStudent
        {
            ProblemId = problemId,
            StudentId = student.Id,
            SubmittedSolutionText = dto.SolutionText,
            SubmittedSolutionPhotoKey = dto.PhotoFileKey,
            SubmittedAt = now,
        };
        _context.ProblemStudents.Add(submission);
    }
    else
    {
        submission.SubmittedSolutionText = dto.SolutionText;
        submission.SubmittedSolutionPhotoKey = dto.PhotoFileKey;
        submission.SubmittedAt = now;
    }

    await _context.SaveChangesAsync();

    return new SubmitSolutionResponseDto
    {
        SubmissionId = submission.Id,
        ProblemId = submission.ProblemId,
        StudentId = submission.StudentId,
        SolutionText = submission.SubmittedSolutionText,
        PhotoUrl = submission.SubmittedSolutionPhotoKey,
        SubmittedAt = submission.SubmittedAt,
    };
}

    public async Task<IEnumerable<SubmissionDto>> GetSubmissionsForProblem(int problemId)
    {
        bool exists = await _context.Problems.AnyAsync(p => p.ID == problemId);
        if (!exists)
            throw new AppException($"Problem with ID {problemId} not found", StatusCodes.Status404NotFound);

        return await _context.ProblemStudents
            .Where(ps => ps.ProblemId == problemId)
            .Select(ps => new SubmissionDto
            {
                Id = ps.Id,
                ProblemId = ps.ProblemId,
                StudentId = ps.StudentId,
                StudentName = ps.Student.User.UserName,
                SolutionText = ps.SubmittedSolutionText,
                SolutionPhotoUrl = ps.SubmittedSolutionPhotoKey,
                SubmittedAt = ps.SubmittedAt,
                Score = ps.Score,
                Feedback = ps.Feedback,
                GradedAt = ps.GradedAt,
            })
            .ToListAsync();
    }

    public async Task<SubmissionDto> GradeSubmission(int submissionId, GradeSubmissionDto dto)
    {
        var isAdmin = _currentUser.Role == "Admin";
        var trainerId = await _context.Trainers
            .Where(t => t.UserId == _currentUser.UserId)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        if (trainerId == null && !isAdmin)
            throw new AppException("Only an instructor can grade submissions", StatusCodes.Status403Forbidden);

        var submission = await _context.ProblemStudents
            .Include(ps => ps.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(ps => ps.Id == submissionId);

        if (submission == null)
            throw new AppException($"Submission with ID {submissionId} not found", StatusCodes.Status404NotFound);

        submission.Score = dto.Score;
        submission.Feedback = dto.Feedback;
        submission.GradedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new SubmissionDto
        {
            Id = submission.Id,
            ProblemId = submission.ProblemId,
            StudentId = submission.StudentId,
            StudentName = submission.Student.User.UserName,
            SolutionText = submission.SubmittedSolutionText,
            SolutionPhotoUrl = submission.SubmittedSolutionPhotoKey,
            SubmittedAt = submission.SubmittedAt,
            Score = submission.Score,
            Feedback = submission.Feedback,
            GradedAt = submission.GradedAt,
        };
    }
}