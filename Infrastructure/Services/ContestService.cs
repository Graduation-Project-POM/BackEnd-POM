using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MIO.API.Settings;
using MIO.Core.DTOs.ContestDTOs;
using MIO.Core.Interfaces;
using MIO.Infrastructure;
using MIO.Infrastructure.Services;
using MIO2.Core.Entities;
using MIO2.Core.Interfaces;

namespace MIO2.Infrastructure.Services;

public class ContestService:IContestService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public ContestService(AppDbContext dbContext,ICurrentUserService currentUserService)
    {
        _dbContext=dbContext;
        _currentUser=currentUserService;
        
    }
 
    
    public async Task<Contest> CreateContest(createContestDTO createContestDTO)
    {
        var trainerId =await  _dbContext.Trainers.Where(t => t.UserId == _currentUser.UserId).
            Select(t => t.Id).
            FirstOrDefaultAsync();

        if (trainerId == 0)
        {
            throw new AppException($"You must be a Trainer to create a contest", StatusCodes.Status403Forbidden);
        }
        
        
        //Console.WriteLine($"UserId: {_currentUser.UserId}, TrainerId: {trainerId}");
        
        var problems = await _dbContext.Problems
            .Where(p => createContestDTO.ProblemIds.Contains(p.ID))
            .ToListAsync(); 

        var contest = new Contest
        {
            Title = createContestDTO.Title,
            Description = createContestDTO.Description,
            TrainerId = trainerId,
            Status = createContestDTO.Status,
            Problems = problems,
            StartAt = createContestDTO.StartAt,
            EndAt = createContestDTO.EndAt
        };

        _dbContext.Contests.Add(contest);
        await _dbContext.SaveChangesAsync();
        return contest;

    }
    

    public async Task<AddStudentsToContestResponseDTO> AddStudentToContest(int contestId, AddStudentsToContestDTO dto)
    {
        
        var contest = await _dbContext.Contests
            .Include(c => c.ContestStudents)
            .FirstOrDefaultAsync(c => c.Id == contestId);
        if (contest == null)
            throw new AppException($"Contest with ID {contestId} not found", StatusCodes.Status404NotFound);
        
        var existingStudentIds = contest.ContestStudents
            .Select(cs => cs.StudentId)
            .ToList();
        
        var newStudentIds = dto.StudentIds
            .Except(existingStudentIds)  
            .ToList();
        
        var students = await _dbContext.Students
            .Where(s => newStudentIds.Contains(s.Id))
            .ToListAsync();
        
        var missingIds = newStudentIds.Except(students.Select(s => s.Id)).ToList();
        
        var contestStudents = students.Select(s => new ContestStudent
        {
            ContestId = contestId,
            StudentId = s.Id,
            ProblemSolved = 0,
            Score = 0,
            TimeToFinishTheContest = 0
        });
        
        
        _dbContext.ContestStudents.AddRange(contestStudents);
        await _dbContext.SaveChangesAsync();
        
        
        
        return new AddStudentsToContestResponseDTO
        {
            Message = $"{students.Count} students added successfully",
            MissingIds = missingIds
        };
    }
    
    
    public async Task<List<ContestDTO>> GetContests()
    {
        return await _dbContext.Contests
            .Include(c => c.Problems)
            .Include(c => c.ContestStudents)
            .Select(c => new ContestDTO
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                Status = c.Status,
                StartAt = c.StartAt,
                EndAt = c.EndAt,
                TrainerId = c.TrainerId,
                ProblemCount = c.Problems.Count,
                StudentCount = c.ContestStudents.Count
            })
            .ToListAsync();
    }

    public async Task<ContestDTO> GetContestById(int contestId)
    {
        var contest = await _dbContext.Contests
            .Where(c => c.Id == contestId)
            .Select(c => new ContestDTO
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                Status = c.Status,
                StartAt = c.StartAt,
                EndAt = c.EndAt,
                TrainerId = c.TrainerId,
                ProblemCount = c.Problems.Count,
                StudentCount = c.ContestStudents.Count
            })
            .FirstOrDefaultAsync();

        if (contest == null)
            throw new AppException($"Contest with ID {contestId} not found", StatusCodes.Status404NotFound);

        return contest;
    }


    public async Task<ContestsStudentsResponseDTO> GetContestsStudents(int contestId)
    {
        var contest = await _dbContext.Contests
            .Where(c => c.Id == contestId)
            .Select(c => new ContestsStudentsResponseDTO
                {
                    ContestStudents = c.ContestStudents.Select(cs => new ContestStudent
                    {
                        Id = cs.Id,
                        StudentId = cs.StudentId,
                        Student = cs.Student,
                        ContestId = cs.ContestId,
                        ProblemSolved = cs.ProblemSolved,
                        Score = cs.Score,
                        TimeToFinishTheContest = cs.TimeToFinishTheContest
                    }).ToList()
                        })
            .FirstOrDefaultAsync();
        
        if (contest == null)
            throw new AppException($"Contest with ID {contestId} not found", StatusCodes.Status404NotFound);

        return new ContestsStudentsResponseDTO
        {
            ContestStudents=contest.ContestStudents
        };
    }
    
    
    public async Task<ContestDTO> UpdateContest(int contestId, UpdateContestDTO dto)
    {
        var trainerId = await _dbContext.Trainers
            .Where(t => t.UserId == _currentUser.UserId)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();

        if (trainerId == 0)
            throw new AppException("You must be a Trainer to update a contest", StatusCodes.Status403Forbidden);

        var contest = await _dbContext.Contests
            .Include(c => c.Problems)
            .FirstOrDefaultAsync(c => c.Id == contestId);

        if (contest == null)
            throw new AppException($"Contest with ID {contestId} not found", StatusCodes.Status404NotFound);

        if (contest.TrainerId != trainerId)
            throw new AppException("You are not authorized to update this contest", StatusCodes.Status403Forbidden);

        // Update scalar fields only if provided (partial update)
        if (!string.IsNullOrWhiteSpace(dto.Title))
            contest.Title = dto.Title;

        if (!string.IsNullOrWhiteSpace(dto.Description))
            contest.Description = dto.Description;

        if (dto.Status.HasValue)
            contest.Status = dto.Status.Value;

        if (dto.StartAt.HasValue)
            contest.StartAt = dto.StartAt.Value;

        if (dto.EndAt.HasValue)
            contest.EndAt = dto.EndAt.Value;

        // Update problems if provided
        if (dto.ProblemIds != null && dto.ProblemIds.Any())
        {
            var problems = await _dbContext.Problems
                .Where(p => dto.ProblemIds.Contains(p.ID))
                .ToListAsync();

            contest.Problems = problems;
        }

        await _dbContext.SaveChangesAsync();

        return new ContestDTO
        {
            Id = contest.Id,
            Title = contest.Title,
            Description = contest.Description,
            Status = contest.Status,
            StartAt = contest.StartAt,
            EndAt = contest.EndAt,
            TrainerId = contest.TrainerId,
            ProblemCount = contest.Problems.Count,
            StudentCount = contest.ContestStudents?.Count ?? 0
        };
    }
    
    
    public async Task DeleteContest(int contestId)
    {
        var trainerId = await _dbContext.Trainers
            .Where(t => t.UserId == _currentUser.UserId)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();

        if (trainerId == 0)
            throw new AppException("You must be a Trainer to delete a contest", StatusCodes.Status403Forbidden);

        var contest = await _dbContext.Contests
            .FirstOrDefaultAsync(c => c.Id == contestId);

        if (contest == null)
            throw new AppException($"Contest with ID {contestId} not found", StatusCodes.Status404NotFound);

        if (contest.TrainerId != trainerId)
            throw new AppException("You are not authorized to delete this contest", StatusCodes.Status403Forbidden);

        _dbContext.Contests.Remove(contest);
        await _dbContext.SaveChangesAsync();
    }
    
}