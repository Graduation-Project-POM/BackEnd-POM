using System.Text.Json;
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

    private static List<ProblemExampleDto>? DeserializeExamples(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<List<ProblemExampleDto>>(json); }
        catch { return null; }
    }

    private static string? SerializeExamples(List<ProblemExampleDto>? examples)
    {
        if (examples == null || examples.Count == 0) return null;
        return JsonSerializer.Serialize(examples);
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

        var query = _context.Problems.Where(p => p.IsBankProblem);

        if (role == "Student")
        {
            query = query.Where(p => p.IsPublished);
        }
        else if (role == "Trainer")
        {
            query = query.Where(p => p.IsPublished || (myTrainerId != null && p.TrainerID == myTrainerId));
        }

        var rows = await query.Select(p => new
        {
            p.ID,
            p.Title,
            p.Statement,
            p.Constraints,
            ExamplesJson = p.Examples,
            p.Difficulty,
            PhotoUrl = string.IsNullOrEmpty(p.PhotoFileKey) ? null : p.PhotoFileKey,
            p.Topic,
            p.IsPublished,
            PublisherName = p.Trainer != null ? p.Trainer.User.FirstName + " " + p.Trainer.User.LastName : null,
            CanModify = isAdmin || (myTrainerId != null && p.TrainerID == myTrainerId)
        }).ToListAsync();

        return rows.Select(r => new ProblemResponseDTO
        {
            ID = r.ID,
            Title = r.Title,
            Statement = r.Statement,
            Constraints = r.Constraints,
            Examples = DeserializeExamples(r.ExamplesJson),
            Difficulty = r.Difficulty,
            PhotoUrl = r.PhotoUrl,
            Topic = r.Topic,
            IsPublished = r.IsPublished,
            PublisherName = r.PublisherName,
            CanModify = r.CanModify
        });
    }

    public async Task<ProblemResponseDTO> GetById(int id)
    {
        var p = await _context.Problems
            .Include(x => x.Trainer).ThenInclude(t => t.User)
            .FirstOrDefaultAsync(x => x.ID == id);

        if (p == null || !p.IsBankProblem)
        {
            // Contest problems are not exposed through the Problem Bank.
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

        return new ProblemResponseDTO
        {
            ID = p.ID,
            Title = p.Title,
            Statement = p.Statement,
            Constraints = p.Constraints,
            Examples = DeserializeExamples(p.Examples),
            Difficulty = p.Difficulty,
            Topic = p.Topic,
            IsPublished = p.IsPublished,
            PublisherName = p.Trainer != null ? p.Trainer.User.FirstName + " " + p.Trainer.User.LastName : null,
            CanModify = isAdmin || (myTrainerId != null && p.TrainerID == myTrainerId),
            PhotoUrl = string.IsNullOrEmpty(p.PhotoFileKey) ? null : p.PhotoFileKey
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
            Constraints = dto.Constraints,
            Examples = SerializeExamples(dto.Examples),
            Difficulty = dto.Difficulty,
            Topic = dto.Topic,
            PhotoFileKey = dto.PhotoFileKey,
            TrainerID = trainer?.Id,
            IsPublished = dto.IsPublished!.Value,
            IsBankProblem = dto.IsBankProblem,
            CreatedAt = DateTime.UtcNow
        };

        _context.Problems.Add(problem);
        await _context.SaveChangesAsync();

        return problem.ID;
    }

    public async Task<bool> Update(int id, UpdateProblemDto dto)
    {
        var problem = await _context.Problems.FindAsync(id);

        if (problem == null || !problem.IsBankProblem)
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
        problem.Constraints = dto.Constraints;
        problem.Examples = SerializeExamples(dto.Examples);
        problem.Difficulty = dto.Difficulty;
        problem.Topic = dto.Topic;
        problem.IsPublished = dto.IsPublished!.Value;
        problem.UpdatedAt = DateTime.UtcNow;

        
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var problem = await _context.Problems.FindAsync(id);

        if (problem == null || !problem.IsBankProblem)
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
        var exists = await _context.Problems.AnyAsync(p => p.ID == problemId);
        if (!exists)
            throw new AppException($"Problem with ID {problemId} not found", StatusCodes.Status404NotFound);

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
                SubmittedAt = ps.SubmittedAt
            })
            .ToListAsync();
    }

    public async Task<SubmissionResponseDto> GradeSubmission(int submissionId, GradeSubmissionDto dto)
    {
        var submission = await _context.ProblemStudents
            .Include(ps => ps.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(ps => ps.Id == submissionId);

        if (submission == null)
            throw new AppException($"Submission with ID {submissionId} not found", StatusCodes.Status404NotFound);

        submission.Score = dto.Grade;
        submission.Feedback = dto.Feedback;
        submission.GradedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new SubmissionResponseDto
        {
            Id = submission.Id,
            ProblemId = submission.ProblemId,
            StudentId = submission.StudentId,
            StudentName = submission.Student.User.FirstName + " " + submission.Student.User.LastName,
            StudentEmail = submission.Student.User.Email,
            SolutionText = submission.SubmittedSolutionText,
            PhotoUrl = submission.SubmittedSolutionPhotoKey,
            Grade = submission.Score,
            Feedback = submission.Feedback,
            GradedAt = submission.GradedAt,
            SubmittedAt = submission.SubmittedAt
        };
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
                ProblemDifficulty = ps.Problem.Difficulty
            })
            .ToListAsync();
    }


    public async Task<BankSubmissionResponseDto> SubmitBankSolution(int problemId, SubmitSolutionDto dto)
    {
        var problem = await _context.Problems.FindAsync(problemId);
        if (problem == null || !problem.IsBankProblem)
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

        var submission = new ProblemBankSubmission
        {
            ProblemId = problemId,
            StudentId = student.Id,
            SolutionText = dto.SolutionText,
            SolutionPhotoKey = dto.PhotoFileKey,
            SubmittedAt = DateTime.UtcNow
        };

        _context.ProblemBankSubmissions.Add(submission);
        await _context.SaveChangesAsync();

        return new BankSubmissionResponseDto
        {
            Id = submission.Id,
            ProblemId = submission.ProblemId,
            StudentId = submission.StudentId,
            SolutionText = submission.SolutionText,
            PhotoUrl = submission.SolutionPhotoKey,
            SubmittedAt = submission.SubmittedAt
        };
    }

    public async Task<IEnumerable<BankSubmissionResponseDto>> GetBankSubmissions(int problemId)
    {
        var problem = await _context.Problems.FirstOrDefaultAsync(p => p.ID == problemId);
        if (problem == null || !problem.IsBankProblem)
            throw new AppException($"Problem with ID {problemId} not found", StatusCodes.Status404NotFound);

        return await _context.ProblemBankSubmissions
            .Where(s => s.ProblemId == problemId)
            .Include(s => s.Student).ThenInclude(st => st.User)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new BankSubmissionResponseDto
            {
                Id = s.Id,
                ProblemId = s.ProblemId,
                StudentId = s.StudentId,
                StudentName = s.Student.User.FirstName + " " + s.Student.User.LastName,
                StudentEmail = s.Student.User.Email,
                SolutionText = s.SolutionText,
                PhotoUrl = s.SolutionPhotoKey,
                SubmittedAt = s.SubmittedAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<BankSubmissionResponseDto>> GetMyBankSubmissions()
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == _currentUser.UserId);
        if (student == null)
            return new List<BankSubmissionResponseDto>();

        return await _context.ProblemBankSubmissions
            .Where(s => s.StudentId == student.Id)
            .Include(s => s.Student).ThenInclude(st => st.User)
            .Include(s => s.Problem)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new BankSubmissionResponseDto
            {
                Id = s.Id,
                ProblemId = s.ProblemId,
                StudentId = s.StudentId,
                StudentName = s.Student.User.FirstName + " " + s.Student.User.LastName,
                StudentEmail = s.Student.User.Email,
                SolutionText = s.SolutionText,
                PhotoUrl = s.SolutionPhotoKey,
                SubmittedAt = s.SubmittedAt,
                ProblemTitle = s.Problem.Title,
                ProblemTopic = s.Problem.Topic,
                ProblemStatement = s.Problem.Statement,
                ProblemDifficulty = s.Problem.Difficulty
            })
            .ToListAsync();
    }


    public async Task<IEnumerable<MySolvedProblemSummaryDto>> GetMySolvedProblems()
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == _currentUser.UserId);
        if (student == null) return new List<MySolvedProblemSummaryDto>();

        var bankRows = await _context.ProblemBankSubmissions
            .Where(s => s.StudentId == student.Id)
            .GroupBy(s => s.ProblemId)
            .Select(g => new
            {
                ProblemId = g.Key,
                Latest = g.Max(s => s.SubmittedAt)
            })
            .ToListAsync();

        var contestRows = await _context.ProblemStudents
            .Where(s => s.StudentId == student.Id)
            .GroupBy(s => s.ProblemId)
            .Select(g => new
            {
                ProblemId = g.Key,
                Latest = g.Max(s => s.SubmittedAt),
                LatestScore = g.OrderByDescending(s => s.SubmittedAt).Select(s => s.Score).FirstOrDefault()
            })
            .ToListAsync();

        var ids = bankRows.Select(b => b.ProblemId)
            .Union(contestRows.Select(c => c.ProblemId))
            .ToList();
        if (ids.Count == 0) return new List<MySolvedProblemSummaryDto>();

        var problems = await _context.Problems
            .Where(p => ids.Contains(p.ID))
            .Select(p => new { p.ID, p.Title, p.Topic, p.Difficulty })
            .ToListAsync();

        var bankMap = bankRows.ToDictionary(b => b.ProblemId);
        var contestMap = contestRows.ToDictionary(c => c.ProblemId);

        return problems
            .Select(p =>
            {
                bankMap.TryGetValue(p.ID, out var b);
                contestMap.TryGetValue(p.ID, out var c);
                DateTime latest;
                if (b != null && c != null) latest = b.Latest > c.Latest ? b.Latest : c.Latest;
                else if (b != null) latest = b.Latest;
                else latest = c!.Latest;
                return new MySolvedProblemSummaryDto
                {
                    ProblemId = p.ID,
                    Title = p.Title,
                    Topic = p.Topic,
                    Difficulty = p.Difficulty,
                    HasBankSubmission = b != null,
                    HasContestSubmission = c != null,
                    LatestContestScore = c?.LatestScore,
                    LatestSubmittedAt = latest
                };
            })
            .OrderByDescending(s => s.LatestSubmittedAt)
            .ToList();
    }

    public async Task<MyProblemSolutionsDto?> GetMyProblemSolutions(int problemId)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == _currentUser.UserId);
        if (student == null) return null;

        var problem = await _context.Problems
            .Where(p => p.ID == problemId)
            .Select(p => new { p.ID, p.Title, p.Statement, p.Topic, p.Difficulty })
            .FirstOrDefaultAsync();
        if (problem == null) return null;

        var bank = await _context.ProblemBankSubmissions
            .Where(s => s.StudentId == student.Id && s.ProblemId == problemId)
            .Include(s => s.Student).ThenInclude(st => st.User)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new BankSubmissionResponseDto
            {
                Id = s.Id,
                ProblemId = s.ProblemId,
                StudentId = s.StudentId,
                StudentName = s.Student.User.FirstName + " " + s.Student.User.LastName,
                StudentEmail = s.Student.User.Email,
                SolutionText = s.SolutionText,
                PhotoUrl = s.SolutionPhotoKey,
                SubmittedAt = s.SubmittedAt
            })
            .ToListAsync();

        var contest = await _context.ProblemStudents
            .Where(s => s.StudentId == student.Id && s.ProblemId == problemId)
            .Include(s => s.Student).ThenInclude(st => st.User)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new SubmissionResponseDto
            {
                Id = s.Id,
                ProblemId = s.ProblemId,
                StudentId = s.StudentId,
                StudentName = s.Student.User.FirstName + " " + s.Student.User.LastName,
                StudentEmail = s.Student.User.Email,
                SolutionText = s.SubmittedSolutionText,
                PhotoUrl = s.SubmittedSolutionPhotoKey,
                Grade = s.Score,
                Feedback = s.Feedback,
                GradedAt = s.GradedAt,
                SubmittedAt = s.SubmittedAt
            })
            .ToListAsync();

        if (bank.Count == 0 && contest.Count == 0) return null;

        return new MyProblemSolutionsDto
        {
            ProblemId = problem.ID,
            Title = problem.Title,
            Statement = problem.Statement,
            Topic = problem.Topic,
            Difficulty = problem.Difficulty,
            BankSubmissions = bank,
            ContestSubmissions = contest
        };
    }

    public async Task<MyProblemSolutionsDto?> GetAllProblemSolutions(int problemId)
    {
        var problem = await _context.Problems
            .Where(p => p.ID == problemId)
            .Select(p => new { p.ID, p.Title, p.Statement, p.Topic, p.Difficulty })
            .FirstOrDefaultAsync();
        if (problem == null) return null;

        var myStudentId = _currentUser.Role == "Student"
            ? (await _context.Students
                .Where(s => s.UserId == _currentUser.UserId)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync())
            : null;

        var bank = await _context.ProblemBankSubmissions
            .Where(s => s.ProblemId == problemId)
            .Include(s => s.Student).ThenInclude(st => st.User)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new BankSubmissionResponseDto
            {
                Id = s.Id,
                ProblemId = s.ProblemId,
                StudentId = s.StudentId,
                StudentName = s.Student.User.FirstName + " " + s.Student.User.LastName,
                StudentEmail = s.Student.User.Email,
                SolutionText = s.SolutionText,
                PhotoUrl = s.SolutionPhotoKey,
                SubmittedAt = s.SubmittedAt,
                IsMine = myStudentId != null && s.StudentId == myStudentId.Value
            })
            .ToListAsync();

        var contest = await _context.ProblemStudents
            .Where(s => s.ProblemId == problemId)
            .Include(s => s.Student).ThenInclude(st => st.User)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new SubmissionResponseDto
            {
                Id = s.Id,
                ProblemId = s.ProblemId,
                StudentId = s.StudentId,
                StudentName = s.Student.User.FirstName + " " + s.Student.User.LastName,
                StudentEmail = s.Student.User.Email,
                SolutionText = s.SubmittedSolutionText,
                PhotoUrl = s.SubmittedSolutionPhotoKey,
                Grade = s.Score,
                Feedback = s.Feedback,
                GradedAt = s.GradedAt,
                SubmittedAt = s.SubmittedAt,
                IsMine = myStudentId != null && s.StudentId == myStudentId.Value
            })
            .ToListAsync();

        return new MyProblemSolutionsDto
        {
            ProblemId = problem.ID,
            Title = problem.Title,
            Statement = problem.Statement,
            Topic = problem.Topic,
            Difficulty = problem.Difficulty,
            BankSubmissions = bank,
            ContestSubmissions = contest
        };
    }

    public async Task<IEnumerable<BankSubmissionResponseDto>> GetMyBankSubmissionsForProblem(int problemId)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == _currentUser.UserId);
        if (student == null)
            return new List<BankSubmissionResponseDto>();

        return await _context.ProblemBankSubmissions
            .Where(s => s.StudentId == student.Id && s.ProblemId == problemId)
            .Include(s => s.Student).ThenInclude(st => st.User)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new BankSubmissionResponseDto
            {
                Id = s.Id,
                ProblemId = s.ProblemId,
                StudentId = s.StudentId,
                StudentName = s.Student.User.FirstName + " " + s.Student.User.LastName,
                StudentEmail = s.Student.User.Email,
                SolutionText = s.SolutionText,
                PhotoUrl = s.SolutionPhotoKey,
                SubmittedAt = s.SubmittedAt
            })
            .ToListAsync();
    }
}