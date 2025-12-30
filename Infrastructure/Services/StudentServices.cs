using Microsoft.EntityFrameworkCore;
using MIO.API.Settings;
using MIO.Core.DTOs.StudentDTO;
using MIO.Core.DTOs.StudentDtos;
using MIO.Core.DTOs.UserDtos;
using MIO.Core.Entities;
using MIO.Core.Interfaces;

namespace MIO.Infrastructure.Services
{
    public class StudentServices : IStudentService
    {
        private readonly AppDbContext _dbContext;

        public StudentServices(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreateStudent(CreateStudentDTO studentDTO)
        {
           
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

            // add role verification here if needed
           

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