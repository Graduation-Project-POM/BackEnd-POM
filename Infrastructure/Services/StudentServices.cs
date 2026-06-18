using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MIO.API.Settings;
using MIO.Core.DTOs.StudentDTO;
using MIO.Core.DTOs.StudentDtos;
using MIO.Core.DTOs.UserDtos;
using MIO.Core.Entities;
using MIO.Core.Enums;
using MIO.Core.Interfaces;
using MIO2.Core.DTOs.StudentDtos;
using MIO2.Core.Events.EventBus;
using MIO2.Core.Events.Students;

namespace MIO.Infrastructure.Services
{
    public class StudentServices : IStudentService
    {
        private readonly AppDbContext _dbContext;
        private readonly ICurrentUserService _currentUser;
      
        
        public StudentServices(AppDbContext dbContext, ICurrentUserService currentUser)
        {
            _dbContext = dbContext;
            _currentUser= currentUser;
        }

        public async Task<Student?> CreateStudent(CreateStudentDTO studentDTO)
        {
            var user=await _dbContext.Users.FirstOrDefaultAsync(u=>u.Id==studentDTO.UserId);
            
            if (!await _dbContext.Users.AnyAsync(u => u.Id == studentDTO.UserId))
            {
                throw new AppException(
                    "There is no user with the ID you've entered, please create a new user or use another user ID",
                    StatusCodes.Status404NotFound);
            }

           
            if (await _dbContext.Students.AnyAsync(s => s.UserId == studentDTO.UserId))
            {
                throw new AppException(
                    "This user is already a student, choose another user ID",
                    StatusCodes.Status409Conflict);
            }

            if (user.RoleID != Role.Student)
            {
                throw new AppException("this user is not a student!"
                    ,StatusCodes.Status409Conflict);
            }
           

            var student = new Student
            {
                GradeLevel = studentDTO.GradeLevel,
                City = studentDTO.City,
                SchoolName = studentDTO.SchoolName,
                NationalId = studentDTO.NationalId, 
                UserId = studentDTO.UserId
            };

            _dbContext.Students.Add(student);
            await _dbContext.SaveChangesAsync();
            return student;
        }

        public async Task<StudentResponseDTO?> GetStudentByIDService(int studentId)
        {
            var student = await _dbContext.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                throw new AppException(
                    $"Student with ID {studentId} not found",
                    StatusCodes.Status404NotFound);
            
            
            return new StudentResponseDTO
            {
                Id = student.Id,
                GradeLevel = student.GradeLevel,
                City = student.City,
                SchoolName = student.SchoolName,
                NationalId = student.NationalId,
                ProblemSolved = student.ProblemSolved,
                Score = student.Score,
                JoinYear = student.JoinYear,
                IsAccepted = student.IsAccepted,
                BasicUser = new BasicUserDTO
                {
                    Id = student.UserId,
                    FirstName = student.User.FirstName,
                    LastName = student.User.LastName,
                    Email = student.User.Email,
                    UserName = student.User.UserName,
                    PhoneNumber = student.User.PhoneNumber
                }
            };
        }

        public async Task<IEnumerable<StudentResponseDTO>> GetStudentsService()
        {
            var records = await _dbContext.Students
                .Include(s => s.User)
                .ToListAsync();

            
            return records.Select(student => new StudentResponseDTO
            {   
                Id = student.Id,
                GradeLevel = student.GradeLevel,
                City = student.City,
                SchoolName = student.SchoolName,
                NationalId = student.NationalId,
                ProblemSolved = student.ProblemSolved,
                Score = student.Score,
                JoinYear = student.JoinYear,
                IsAccepted = student.IsAccepted,
                BasicUser = new BasicUserDTO
                {
                    Id = student.UserId,
                    FirstName = student.User.FirstName,
                    LastName = student.User.LastName,
                    Email = student.User.Email,
                    UserName = student.User.UserName,
                    PhoneNumber = student.User.PhoneNumber
                }
            });
        }

        public async Task<MultipleStudentsResponseDTO> GetMultipleStudentsService(IEnumerable<int> ids)
        {
            var idList = ids.ToList();

            var students = await _dbContext.Students
                .Where(s => idList.Contains(s.Id))
                .Include(s => s.User)
                .ToListAsync();

            var foundIds = students.Select(s => s.Id).ToList();
            var missingIds = idList.Except(foundIds).ToList();

            return new MultipleStudentsResponseDTO
            {
                Students = students.Select(student => new StudentResponseDTO
                {
                    Id = student.Id,
                    GradeLevel = student.GradeLevel,
                    City = student.City,
                    SchoolName = student.SchoolName,
                    NationalId = student.NationalId,
                    ProblemSolved = student.ProblemSolved,
                    Score = student.Score,
                    JoinYear = student.JoinYear,
                    IsAccepted = student.IsAccepted,
                    BasicUser = new BasicUserDTO
                    {
                        Id = student.UserId,
                        FirstName = student.User.FirstName,
                        LastName = student.User.LastName,
                        Email = student.User.Email,
                        UserName = student.User.UserName,
                        PhoneNumber = student.User.PhoneNumber
                    }
                }),
                MissingIds = missingIds
            };
        }
        
        public async Task<Student?> UpdateStudentService(int id, UpdateStudentDTO studentDTO)
        {
            var existingStudent = await _dbContext.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existingStudent == null)
            {
                throw new AppException(
                    $"Student with ID {id} not found",
                    StatusCodes.Status404NotFound);
            }

            existingStudent.GradeLevel = studentDTO.GradeLevel;
            existingStudent.City = studentDTO.City;
            existingStudent.SchoolName = studentDTO.SchoolName;
            existingStudent.NationalId = studentDTO.NationalId;
           

            _dbContext.Students.Update(existingStudent);
            await _dbContext.SaveChangesAsync();

            return existingStudent;
        }

        private async Task<Student> GetOrCreateCurrentStudent()
        {
            var userId = _currentUser.UserId;
            var student = await _dbContext.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null || user.RoleID != Role.Student)
                    throw new AppException("Only students have a student profile", StatusCodes.Status403Forbidden);

                student = new Student
                {
                    UserId = userId,
                    GradeLevel = "",
                    City = "",
                    SchoolName = "",
                    NationalId = "",
                    IsAccepted = true
                };
                _dbContext.Students.Add(student);
                await _dbContext.SaveChangesAsync();
                student.User = user;
            }

            return student;
        }

        private static StudentResponseDTO MapToResponse(Student student) => new StudentResponseDTO
        {
            Id = student.Id,
            GradeLevel = student.GradeLevel,
            City = student.City,
            SchoolName = student.SchoolName,
            NationalId = student.NationalId,
            ProblemSolved = student.ProblemSolved,
            Score = student.Score,
            JoinYear = student.JoinYear,
            IsAccepted = student.IsAccepted,
            BasicUser = student.User == null ? null! : new BasicUserDTO
            {
                Id = student.UserId,
                FirstName = student.User.FirstName,
                LastName = student.User.LastName,
                Email = student.User.Email,
                UserName = student.User.UserName,
                PhoneNumber = student.User.PhoneNumber
            }
        };

        public async Task<StudentResponseDTO?> GetMyStudentService()
        {
            var student = await GetOrCreateCurrentStudent();
            return MapToResponse(student);
        }

        public async Task<Student?> UpdateMyStudentService(UpdateStudentDTO studentDTO)
        {
            var student = await GetOrCreateCurrentStudent();

            student.GradeLevel = studentDTO.GradeLevel;
            student.City = studentDTO.City;
            student.SchoolName = studentDTO.SchoolName;
            student.NationalId = studentDTO.NationalId;

            await _dbContext.SaveChangesAsync();
            return student;
        }

        public async Task<Student?> DeleteStudentService(int id)
        {
            var existingStudent = await _dbContext.Students.FindAsync(id);

            if (existingStudent == null)
                throw new AppException(
                    $"Student with ID {id} not found",
                    StatusCodes.Status404NotFound);

            _dbContext.Students.Remove(existingStudent);
            await _dbContext.SaveChangesAsync();

            return existingStudent;
        }

        
        public async Task<IEnumerable<StudentResponseDTO>> GetAcceptedStudentsService()
        {
            var records= await _dbContext.Students
                .Where(s => s.IsAccepted)
                .Include(s => s.User)
                .ToListAsync();

            return records.Select(student => new StudentResponseDTO
            {
                Id = student.Id,
                GradeLevel = student.GradeLevel,
                City = student.City,
                SchoolName = student.SchoolName,
                NationalId = student.NationalId,
                ProblemSolved = student.ProblemSolved,
                Score = student.Score,
                JoinYear = student.JoinYear,
                IsAccepted = student.IsAccepted,
                BasicUser = new BasicUserDTO
                {
                    Id = student.UserId,
                    FirstName = student.User.FirstName,
                    LastName = student.User.LastName,
                    Email = student.User.Email,
                    UserName = student.User.UserName,
                    PhoneNumber = student.User.PhoneNumber
                }
            });

        }

        public async Task<IEnumerable<StudentResponseDTO>> GetStudentsByCityService(string city)
        {
           var records= await _dbContext.Students
                .Where(s => s.City == city)
                .Include(s => s.User)
                .ToListAsync();


            return records.Select(student => new StudentResponseDTO
            {
                Id = student.Id,
                GradeLevel = student.GradeLevel,
                City = student.City,
                SchoolName = student.SchoolName,
                NationalId = student.NationalId,
                ProblemSolved = student.ProblemSolved,
                Score = student.Score,
                JoinYear = student.JoinYear,
                IsAccepted = student.IsAccepted,
                BasicUser = new BasicUserDTO
                {
                    Id = student.UserId,
                    FirstName = student.User.FirstName,
                    LastName = student.User.LastName,
                    Email = student.User.Email,
                    UserName = student.User.UserName,
                    PhoneNumber = student.User.PhoneNumber
                }
            });
        }

        public async Task<StudentResponseDTO?> AcceptStudentService(int studentId)
        {
            var student = await _dbContext.Students
                              .Include(s => s.User)  
                              .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                throw new AppException(
                    $"Student with ID {studentId} not found",
                    StatusCodes.Status404NotFound);

            student.IsAccepted = true;
            await _dbContext.SaveChangesAsync();

             await studentEvents.RaiseAccepted
                (
                    new StudentAcceptedEvent
                    (
                     student.Id,
                     student.User.Email,
                     student.User.FirstName
                    )
                );


            return  new StudentResponseDTO
            {
                Id = student.Id,
                GradeLevel = student.GradeLevel,
                City = student.City,
                SchoolName = student.SchoolName,
                NationalId = student.NationalId,
                ProblemSolved = student.ProblemSolved,
                Score = student.Score,
                JoinYear = student.JoinYear,
                IsAccepted = student.IsAccepted,
                BasicUser = new BasicUserDTO
                {
                    Id = student.UserId,
                    FirstName = student.User.FirstName,
                    LastName = student.User.LastName,
                    Email = student.User.Email,
                    UserName = student.User.UserName,
                    PhoneNumber = student.User.PhoneNumber
                }
            };
        }
    }
}