using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MIO.API.Settings;
using MIO.Core.DTOs.ProblemDTOs;
using MIO.Core.Entities;
using MIO.Core.Enums;
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
       

        var role = _currentUser.Role;
        var isAdmin = role == "Admin";

        int? myTrainerId = null;
        if (role == "Trainer")
        {
            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t => t.UserId == _currentUser.UserId);
            myTrainerId = trainer?.Id;
        }

        var query = _context.Problems.Where(p => p.IsBankVisible);

        if (role == "Student")
        {
            query = query.Where(p => p.IsPublished);
        }
        else if (role == "Trainer")
        {
            query = query.Where(p => p.IsPublished || (myTrainerId != null && p.TrainerID == myTrainerId));
        }

        return await query.Select(p => new ProblemResponseDTO
        {
            ID = p.ID,
            Title = p.Title,
            Statement = p.Statement,
            Difficulty = p.Difficulty,
            // Relative path (e.g. "/uploads/x.png"); the client resolves it
            // against the API host. Null when the problem has no image.
            PhotoUrl = string.IsNullOrEmpty(p.PhotoFileKey) ? null : p.PhotoFileKey,
            Topic = p.Topic,
            IsPublished = p.IsPublished,
            PublisherName = p.Trainer != null ? p.Trainer.User.FirstName + " " + p.Trainer.User.LastName : null,
            CanModify = isAdmin || (myTrainerId != null && p.TrainerID == myTrainerId),
            Points = p.Points,
            SolvedCount = p.SolvedCount

        })   .ToListAsync();;
    }

    public async Task<ProblemResponseDTO> GetById(int id)
    {
        var p = await _context.Problems
            .Include(x => x.Trainer).ThenInclude(t => t.User)
            .FirstOrDefaultAsync(x => x.ID == id);

        if (p == null)
        {
            throw new AppException(
                $"Problem with ID {id} not found",
                StatusCodes.Status404NotFound);
        }

        var role = _currentUser.Role;
        var isAdmin = role == "Admin";
        int? myTrainerId = null;
        if (role == "Trainer")
        {
            myTrainerId = (await _context.Trainers
                .FirstOrDefaultAsync(t => t.UserId == _currentUser.UserId))?.Id;
        }

        if (role == "Student" && !p.IsPublished)
        {
            throw new AppException(
                $"Problem with ID {id} not found",
                StatusCodes.Status404NotFound);
        }

        if (role == "Trainer" && !p.IsPublished && (myTrainerId == null || p.TrainerID != myTrainerId))
        {
            throw new AppException(
                $"Problem with ID {id} not found",
                StatusCodes.Status404NotFound);
        }

        if (!p.IsBankVisible)
        {
            var ownsProblem = myTrainerId != null && p.TrainerID == myTrainerId;
            if (!isAdmin && !ownsProblem)
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
            PublisherName = p.Trainer != null ? p.Trainer.User.FirstName + " " + p.Trainer.User.LastName : null,
            CanModify = isAdmin || (myTrainerId != null && p.TrainerID == myTrainerId),
            PhotoUrl = string.IsNullOrEmpty(p.PhotoFileKey) ? null : p.PhotoFileKey,
            Points = p.Points,
            SolvedCount = p.SolvedCount
        };
    }

    public async Task<int> Create(CreateProblemDto dto)
    {
       var userID=_currentUser.UserId;
       var isAdmin = _currentUser.Role == "Admin";
       var trainer=await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == userID);

        if (trainer == null && !isAdmin)
        {
            if (_currentUser.Role == "Trainer")
            {
                trainer = new Trainer { UserId = userID, Specialization = "" };
                _context.Trainers.Add(trainer);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new AppException(
                    "Invalid TrainerID. Trainer does not exist.",
                    StatusCodes.Status404NotFound);
            }
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
            Statement = dto.Statement ?? "",
            Difficulty = dto.Difficulty,
            Topic = dto.Topic,
            PhotoFileKey = dto.PhotoFileKey,
            TrainerID = trainer?.Id,
            IsPublished = dto.IsPublished,
            Points = dto.Points,                
            IsBankVisible = dto.IsBankVisible,    
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
        problem.Statement = dto.Statement ?? problem.Statement;
        problem.Difficulty = dto.Difficulty;
        problem.Topic = dto.Topic;
        problem.IsPublished = dto.IsPublished;
        problem.Points = dto.Points;           
        problem.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        problem.SolvedCount = await _context.ProblemStudents
            .CountAsync(ps => ps.ProblemId == problem.ID && ps.Score == problem.Points);
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

        if (_currentUser.Role == "Trainer")
        {
            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t => t.UserId == _currentUser.UserId);
            if (trainer == null || problem.TrainerID != trainer.Id)
            {
                throw new AppException(
                    "You can only delete problems you created",
                    StatusCodes.Status403Forbidden);
            }
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
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.RoleID != Role.Student)
                throw new AppException("Only students can submit solutions", StatusCodes.Status403Forbidden);

            student = new Student
            {
                UserId = userId,
                GradeLevel = "",
                City = "",
                SchoolName = "",
                NationalId = "",
                IsAccepted = true
            };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
        }

        if (string.IsNullOrWhiteSpace(dto.SolutionText) && string.IsNullOrWhiteSpace(dto.PhotoFileKey))
            throw new AppException("You must provide a solution text or a photo", StatusCodes.Status400BadRequest);

        bool alreadySubmitted = await _context.ProblemStudents
            .AnyAsync(ps => ps.ProblemId == problemId && ps.StudentId == student.Id);
        if (alreadySubmitted)
            throw new AppException("You have already submitted a solution for this problem", StatusCodes.Status409Conflict);

        var submission = new ProblemStudent
        {
            ProblemId = problemId,
            StudentId = student.Id,
            SubmittedSolutionText = dto.SolutionText,
            SubmittedSolutionPhotoKey = dto.PhotoFileKey,
            SubmittedAt = DateTime.UtcNow
        };

        _context.ProblemStudents.Add(submission);
        await _context.SaveChangesAsync();

        return new SubmitSolutionResponseDto
        {
            SubmissionId = submission.Id,
            ProblemId = submission.ProblemId,
            StudentId = submission.StudentId,
            SolutionText = submission.SubmittedSolutionText,
            PhotoUrl = submission.SubmittedSolutionPhotoKey,
            SubmittedAt = submission.SubmittedAt
        };
    }

    public async Task<IEnumerable<SubmissionResponseDto>> GetSubmissions(int problemId)
    {
        var problem = await _context.Problems.FindAsync(problemId);
        if (problem == null)
            throw new AppException($"Problem with ID {problemId} not found", StatusCodes.Status404NotFound);

        await EnsureCanGradeAsync(problem);

        return await _context.ProblemStudents
            .Where(ps => ps.ProblemId == problemId)
            .Include(ps => ps.Student).ThenInclude(s => s.User)
            .OrderByDescending(ps => ps.SubmittedAt)
            .Select(ps => new SubmissionResponseDto
            {
                Id = ps.Id,
                ProblemId = ps.ProblemId,
                StudentId = ps.StudentId,
                StudentName = ps.Student.User.FirstName + " " + ps.Student.User.LastName,
                StudentEmail = ps.Student.User.Email,
                SolutionText = ps.SubmittedSolutionText,
                PhotoUrl = ps.SubmittedSolutionPhotoKey,
                Grade = ps.Score,
                Feedback = ps.Feedback,
                GradedAt = ps.GradedAt,
                SubmittedAt = ps.SubmittedAt,
                ProblemPoints = problem.Points
            })
            .ToListAsync();
    }

    public async Task<SubmissionResponseDto> GradeSubmission(int submissionId, GradeSubmissionDto dto)
    {
        var submission = await _context.ProblemStudents
            .Include(ps => ps.Student).ThenInclude(s => s.User)
            .Include(ps => ps.Problem)
            .FirstOrDefaultAsync(ps => ps.Id == submissionId);

        if (submission == null)
            throw new AppException($"Submission with ID {submissionId} not found", StatusCodes.Status404NotFound);

        var problem = submission.Problem;

        await EnsureCanGradeAsync(problem);

        if (dto.Grade < 0 || dto.Grade > problem.Points)
            throw new AppException(
                "GRADE_OUT_OF_RANGE",
                $"Grade must be between 0 and {problem.Points} for this problem.",
                StatusCodes.Status400BadRequest);

        submission.Score = dto.Grade;
        submission.Feedback = dto.Feedback;
        submission.GradedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await RecomputeAggregatesAsync(problem.ID, submission.StudentId);
        await _context.SaveChangesAsync();

        return MapSubmission(submission, problem.Points);
    }

    public async Task<IEnumerable<SubmissionResponseDto>> GetMySubmissions()
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == _currentUser.UserId);
        if (student == null)
            return new List<SubmissionResponseDto>();

        return await _context.ProblemStudents
            .Where(ps => ps.StudentId == student.Id)
            .Include(ps => ps.Student).ThenInclude(s => s.User)
            .Include(ps => ps.Problem)
            .OrderByDescending(ps => ps.SubmittedAt)
            .Select(ps => new SubmissionResponseDto
            {
                Id = ps.Id,
                ProblemId = ps.ProblemId,
                StudentId = ps.StudentId,
                StudentName = ps.Student.User.FirstName + " " + ps.Student.User.LastName,
                StudentEmail = ps.Student.User.Email,
                SolutionText = ps.SubmittedSolutionText,
                PhotoUrl = ps.SubmittedSolutionPhotoKey,
                Grade = ps.Score,
                Feedback = ps.Feedback,
                GradedAt = ps.GradedAt,
                SubmittedAt = ps.SubmittedAt,
                ProblemTitle = ps.Problem.Title,
                ProblemTopic = ps.Problem.Topic,
                ProblemStatement = ps.Problem.Statement,
                ProblemDifficulty = ps.Problem.Difficulty,
                ProblemPoints = ps.Problem.Points
            })
            .ToListAsync();
    }

    private async Task EnsureCanGradeAsync(Problem problem)
    {
        if (_currentUser.Role == "Admin") return;

        if (_currentUser.Role != "Trainer")
            throw new AppException("FORBIDDEN", "Only trainers can grade submissions.", StatusCodes.Status403Forbidden);

        var myTrainerId = await _context.Trainers
            .Where(t => t.UserId == _currentUser.UserId)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        if (myTrainerId == null)
            throw new AppException("FORBIDDEN", "Only trainers can grade submissions.", StatusCodes.Status403Forbidden);

        var owningContest = await _context.Contests
            .Where(c => c.Problems.Any(p => p.ID == problem.ID))
            .Select(c => new { c.TrainerId })
            .FirstOrDefaultAsync();

        var canGrade = owningContest != null
            ? owningContest.TrainerId == myTrainerId          // contest grading → creator only
            : problem.TrainerID == myTrainerId;               // bank problem → its creator

        if (!canGrade)
            throw new AppException(
                "NOT_CONTEST_OWNER",
                "Only the trainer who created this contest can grade its submissions.",
                StatusCodes.Status403Forbidden);
    }

    private async Task RecomputeAggregatesAsync(int problemId, int studentId)
    {
        var problem = await _context.Problems.FindAsync(problemId);
        if (problem != null)
        {
            problem.SolvedCount = await _context.ProblemStudents
                .CountAsync(ps => ps.ProblemId == problemId && ps.Score == problem.Points);
        }

        var student = await _context.Students.FindAsync(studentId);
        if (student != null)
        {

            student.TotalPoints = await _context.ProblemStudents
                .Where(ps => ps.StudentId == studentId && ps.Score != null)
                .SumAsync(ps => (int?)ps.Score) ?? 0;

            student.ProblemSolved = await _context.ProblemStudents
                .CountAsync(ps => ps.StudentId == studentId && ps.Score == ps.Problem.Points);
        }
    }

    private static SubmissionResponseDto MapSubmission(ProblemStudent ps, int problemPoints) => new()
    {
        Id = ps.Id,
        ProblemId = ps.ProblemId,
        StudentId = ps.StudentId,
        StudentName = ps.Student.User.FirstName + " " + ps.Student.User.LastName,
        StudentEmail = ps.Student.User.Email,
        SolutionText = ps.SubmittedSolutionText,
        PhotoUrl = ps.SubmittedSolutionPhotoKey,
        Grade = ps.Score,
        Feedback = ps.Feedback,
        GradedAt = ps.GradedAt,
        SubmittedAt = ps.SubmittedAt,
        ProblemPoints = problemPoints
    };
}