using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MIO.API.Settings;
using MIO.Core.DTOs.ProblemDTOs;
using MIO.Core.Entities;
using MIO.Core.Interfaces;
using MIO.Infrastructure;
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
            Difficulty = p.Difficulty,
            Topic = p.Topic,
            IsPublished = p.IsPublished,
            //PhotoUrl = string.IsNullOrEmpty(p.PhotoFileKey) ? null : _s3Service.GetPublicUrl(p.PhotoFileKey)
        };
    }

    public async Task<int> Create(CreateProblemDto dto)
    {
       var userID=_currentUser.UserId;
       var isAdmin = _currentUser.Role == "Admin";
       var trainer=await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == userID);

        // Trainers own the problems they create; admins may also create (no owner).
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

        if (!string.IsNullOrEmpty(dto.PhotoFileKey) && dto.PhotoFileKey != problem.PhotoFileKey)
        {
            // if (!string.IsNullOrEmpty(problem.PhotoFileKey))
            //     await _s3Service.DeleteMediaAsync(problem.PhotoFileKey);

            problem.PhotoFileKey = dto.PhotoFileKey;
        }
        
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

        // if (!string.IsNullOrEmpty(problem.PhotoFileKey))
        //     await _s3Service.DeleteMediaAsync(problem.PhotoFileKey);
        
        _context.Problems.Remove(problem);
        await _context.SaveChangesAsync();

        return true;
    }
    
    
    
    public async Task<SubmitSolutionResponseDto> SubmitSolution(int problemId, SubmitSolutionDto dto)
{
    
    var problem = await _context.Problems.FindAsync(problemId);
    if (problem == null)
        throw new AppException($"Problem with ID {problemId} not found", StatusCodes.Status404NotFound);

   
    var userId = _currentUser.UserId;
    var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
    if (student == null)
        throw new AppException("Student not found for current user", StatusCodes.Status404NotFound);

   
    if (string.IsNullOrWhiteSpace(dto.SolutionText) && string.IsNullOrWhiteSpace(dto.PhotoFileKey))
        throw new AppException("You must provide a solution text or a photo", StatusCodes.Status400BadRequest);

    
    if (!string.IsNullOrWhiteSpace(dto.PhotoFileKey))
    {
        // bool exists = await _s3Service.IsMediaExists(dto.PhotoFileKey);
        // if (!exists)
        //     throw new AppException("Photo file not found in storage", StatusCodes.Status404NotFound);
    }

    // check if already submitted
    bool alreadySubmitted = await _context.ProblemStudents
        .AnyAsync(ps => ps.ProblemId == problemId && ps.StudentId == student.Id);
    if (alreadySubmitted)
        throw new AppException("You have already submitted a solution for this problem", StatusCodes.Status409Conflict);

    // var submission = new ProblemStudent
    // {
    //     ProblemId = problemId,
    //     StudentId = student.Id,
    //     SubmittedSolutionText = dto.SolutionText,
    //     SubmittedSolutionPhotoKey = dto.PhotoFileKey
    // };
    
    
   // _context.ProblemStudents.Add(submission);
    await _context.SaveChangesAsync();

    return new SubmitSolutionResponseDto
    {
        // SubmissionId = submission.Id,
        // ProblemId = submission.ProblemId,
        // StudentId = submission.StudentId,
        // SolutionText = submission.SubmittedSolutionText,
        // PhotoUrl = string.IsNullOrEmpty(submission.SubmittedSolutionPhotoKey)
        //     ? null
        //     : _s3Service.GetPublicUrl(submission.SubmittedSolutionPhotoKey),
        // SubmittedAt = submission.SubmittedAt
    };
}
}