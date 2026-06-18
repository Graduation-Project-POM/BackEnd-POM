using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MIO.API.Settings;
using MIO.Core.DTOs.ContestDTOs;
using MIO.Core.DTOs.ProblemDTOs;
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
        var isAdmin = _currentUser.Role == "Admin";

        var trainerId = await _dbContext.Trainers
            .Where(t => t.UserId == _currentUser.UserId)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        // Trainers own the contests they create; admins may also create (no owner).
        if (trainerId == null && !isAdmin)
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
        var isAdmin = _currentUser.Role == "Admin";

        var trainerId = await _dbContext.Trainers
            .Where(t => t.UserId == _currentUser.UserId)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        var query = _dbContext.Contests.AsQueryable();

        // A trainer sees only contests that belong to a group they own
        // (linked via GroupContest → Group.TrainerId). Admins see all contests.
        if (!isAdmin)
            query = query.Where(c => _dbContext.GroupContests
                .Any(gc => gc.ContestId == c.Id && gc.Group.TrainerId == trainerId));

        return await query
            .Select(c => new ContestDTO
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                Status = c.Status,
                StartAt = c.StartAt,
                EndAt = c.EndAt,
                TrainerId = c.TrainerId ?? 0,
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
                TrainerId = c.TrainerId ?? 0,
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
    
    
    public async Task<List<ProblemResponseDTO>> GetContestProblems(int contestId)
    {
        var exists = await _dbContext.Contests.AnyAsync(c => c.Id == contestId);
        if (!exists)
            throw new AppException($"Contest with ID {contestId} not found", StatusCodes.Status404NotFound);

        return await _dbContext.Contests
            .Where(c => c.Id == contestId)
            .SelectMany(c => c.Problems)
            .Select(p => new ProblemResponseDTO
            {
                ID = p.ID,
                Title = p.Title,
                Difficulty = p.Difficulty,
                Topic = p.Topic,
                PhotoUrl = p.PhotoFileKey,
                IsPublished = p.IsPublished
            })
            .ToListAsync();
    }


    public async Task<List<StudentContestProblemDto>> GetContestProblemsForStudent(int contestId)
    {
        var studentId = await _dbContext.Students
            .Where(s => s.UserId == _currentUser.UserId)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();

        if (studentId == null)
            throw new AppException("Student not found for current user", StatusCodes.Status404NotFound);

        var contestExists = await _dbContext.Contests.AnyAsync(c => c.Id == contestId);
        if (!contestExists)
            throw new AppException($"Contest with ID {contestId} not found", StatusCodes.Status404NotFound);

        // The student may only access a contest that belongs to one of their groups.
        var hasAccess = await _dbContext.GroupContests
            .AnyAsync(gc => gc.ContestId == contestId &&
                            gc.Group.GroupStudents.Any(gs => gs.StudentId == studentId));
        if (!hasAccess)
            throw new AppException("You are not enrolled in a group for this contest", StatusCodes.Status403Forbidden);

        var problems = await _dbContext.Contests
            .Where(c => c.Id == contestId)
            .SelectMany(c => c.Problems)
            .Select(p => new { p.ID, p.Title, p.Statement, p.Difficulty, p.Topic, p.PhotoFileKey })
            .ToListAsync();

        var problemIds = problems.Select(p => p.ID).ToList();
        var subs = await _dbContext.ProblemStudents
            .Where(ps => ps.StudentId == studentId && problemIds.Contains(ps.ProblemId))
            .ToListAsync();
        var subByProblem = subs.ToDictionary(s => s.ProblemId);

        return problems.Select(p =>
        {
            subByProblem.TryGetValue(p.ID, out var s);
            return new StudentContestProblemDto
            {
                ProblemId = p.ID,
                Title = p.Title,
                Statement = p.Statement,
                Difficulty = p.Difficulty,
                Topic = p.Topic,
                PhotoUrl = string.IsNullOrEmpty(p.PhotoFileKey) ? null : p.PhotoFileKey,
                Submitted = s != null,
                SubmissionId = s?.Id,
                SolutionText = s?.SubmittedSolutionText,
                SolutionPhotoUrl = s?.SubmittedSolutionPhotoKey,
                SubmittedAt = s?.SubmittedAt,
                Score = s?.Score,
                Feedback = s?.Feedback,
                GradedAt = s?.GradedAt,
            };
        }).ToList();
    }

    public async Task<ContestDTO> UpdateContest(int contestId, UpdateContestDTO dto)
    {
        var isAdmin = _currentUser.Role == "Admin";

        var trainerId = await _dbContext.Trainers
            .Where(t => t.UserId == _currentUser.UserId)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        if (trainerId == null && !isAdmin)
            throw new AppException("You must be a Trainer to update a contest", StatusCodes.Status403Forbidden);

        var contest = await _dbContext.Contests
            .Include(c => c.Problems)
            .FirstOrDefaultAsync(c => c.Id == contestId);

        if (contest == null)
            throw new AppException($"Contest with ID {contestId} not found", StatusCodes.Status404NotFound);


        if (!isAdmin && !await CanTrainerManageContestAsync(contestId, contest.TrainerId, trainerId))
            throw new AppException("You are not authorized to update this contest", StatusCodes.Status403Forbidden);

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

        if (dto.ProblemIds != null)
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
            TrainerId = contest.TrainerId ?? 0,
            ProblemCount = contest.Problems.Count,
            StudentCount = contest.ContestStudents?.Count ?? 0
        };
    }
    
    
    public async Task DeleteContest(int contestId)
    {
        var isAdmin = _currentUser.Role == "Admin";

        var trainerId = await _dbContext.Trainers
            .Where(t => t.UserId == _currentUser.UserId)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        if (trainerId == null && !isAdmin)
            throw new AppException("You must be a Trainer to delete a contest", StatusCodes.Status403Forbidden);

        var contest = await _dbContext.Contests
            .FirstOrDefaultAsync(c => c.Id == contestId);

        if (contest == null)
            throw new AppException($"Contest with ID {contestId} not found", StatusCodes.Status404NotFound);

        if (!isAdmin && !await CanTrainerManageContestAsync(contestId, contest.TrainerId, trainerId))
            throw new AppException("You are not authorized to delete this contest", StatusCodes.Status403Forbidden);

        _dbContext.Contests.Remove(contest);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<bool> CanTrainerManageContestAsync(int contestId, int? contestTrainerId, int? myTrainerId)
    {
        if (myTrainerId == null) return false;
        if (contestTrainerId == myTrainerId) return true;

        return await _dbContext.GroupContests
            .AnyAsync(gc => gc.ContestId == contestId && gc.Group.TrainerId == myTrainerId);
    }

}