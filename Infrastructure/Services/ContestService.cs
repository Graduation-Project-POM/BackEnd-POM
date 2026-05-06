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
        
        Console.WriteLine($"UserId: {_currentUser.UserId}, TrainerId: {trainerId}");
        
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
    
}