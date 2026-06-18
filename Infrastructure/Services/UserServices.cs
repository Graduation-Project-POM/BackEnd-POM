using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MIO.API.Authentication;
using MIO.API.Settings;
using MIO.Core.DTOs;
using MIO.Core.DTOs.StudentDtos;
using MIO.Core.DTOs.TrainerDtos;
using MIO.Core.DTOs.UserDtos;
using MIO.Core.Entities;
using MIO.Core.Enums;
using MIO.Core.Interfaces;

namespace MIO.Infrastructure.Services
{
    public class UserServices : IUserService
    {
        private readonly AppDbContext _dbContext;
        private readonly ICurrentUserService _currentUser;

        public UserServices(AppDbContext dbContext, ICurrentUserService currentUser)
        {
            _dbContext = dbContext;
            _currentUser = currentUser;
        }

        public async Task<User?> CreateUser(CreateUserDTO userDTO)
        {
            if (await _dbContext.Users.AnyAsync(u => u.UserName == userDTO.UserName))
                throw new AppException("USERNAME_EXISTS", "Username already exists", StatusCodes.Status409Conflict);

            Security.CreatePasswordHash(userDTO.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User
            {
                FirstName = userDTO.FirstName,
                MiddleName = userDTO.MiddleName,
                LastName = userDTO.LastName,
                Email = userDTO.Email,
                UserName = userDTO.UserName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                PhoneNumber = userDTO.PhoneNumber,
                Gender = userDTO.Gender,
                ProfileImageUrl = userDTO.ProfileImageUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                RoleID = userDTO.RoleID
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            await EnsureRoleProfile(user);
            return user;
        }

        // Ensure a user with the Student/Trainer role has the matching entity row,
        // so they appear in GET /Student and GET /Trainer (and can join groups).
        private async Task EnsureRoleProfile(User user)
        {
            if (user.RoleID == Role.Student &&
                !await _dbContext.Students.AnyAsync(s => s.UserId == user.Id))
            {
                _dbContext.Students.Add(new Student
                {
                    UserId = user.Id,
                    GradeLevel = "",
                    City = "",
                    SchoolName = "",
                    NationalId = "",
                    IsAccepted = true
                });
                await _dbContext.SaveChangesAsync();
            }
            else if (user.RoleID == Role.Trainer &&
                !await _dbContext.Trainers.AnyAsync(t => t.UserId == user.Id))
            {
                _dbContext.Trainers.Add(new Trainer
                {
                    UserId = user.Id,
                    Specialization = ""
                });
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<User?> RegisterStudent(CreateUserDTO userDTO)
        {
            if (await _dbContext.Users.AnyAsync(u => u.UserName == userDTO.UserName))
                throw new AppException("USERNAME_EXISTS", "Username already exists", StatusCodes.Status409Conflict);

            Security.CreatePasswordHash(userDTO.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User
            {
                FirstName = userDTO.FirstName,
                MiddleName = userDTO.MiddleName,
                LastName = userDTO.LastName,
                Email = userDTO.Email,
                UserName = userDTO.UserName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                PhoneNumber = userDTO.PhoneNumber,
                Gender = userDTO.Gender,
                ProfileImageUrl = userDTO.ProfileImageUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                RoleID = Role.Student
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            await EnsureRoleProfile(user);
            return user;
        }

        public async Task<User?> GetUserService(AuthenticationRequest request)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);

            if (user == null)
                throw new AppException("INVALID_CREDENTIALS", "Invalid username or password", StatusCodes.Status401Unauthorized);

            if (!Security.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
                throw new AppException("INVALID_CREDENTIALS", "Invalid username or password", StatusCodes.Status401Unauthorized);

            return user;
        }

        public async Task<UserResponseDTO?> GetUserByIDService(int UserId)
        {
            var user = await _dbContext.Users
                .Include(u => u.Trainer)
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.Id == UserId);

            if (user == null)
                throw new AppException("USER_NOT_FOUND", $"User with ID {UserId} not found", StatusCodes.Status404NotFound);

            var userResponse = new UserResponseDTO
            {
                Id = user.Id,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Email = user.Email,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                ProfileImageUrl = user.ProfileImageUrl,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastUpdatedAt = user.LastUpdatedAt,
            };

            if (user.Trainer != null)
                userResponse.BasicTrainer = new BasicTrainerDTO
                {
                    Id = user.Trainer.Id,
                    Specialization = user.Trainer.Specialization
                };

            if (user.Student != null)
                userResponse.BasicStudent = new BasicStudentDTO
                {
                    Id = user.Student.Id,
                    City = user.Student.City,
                    GradeLevel = user.Student.GradeLevel,
                    JoinYear = user.Student.JoinYear,
                    NationalId = user.Student.NationalId,
                    ProblemSolved = user.Student.ProblemSolved,
                    SchoolName = user.Student.SchoolName,
                    Score = user.Student.Score
                };

            return userResponse;
        }

        public async Task<IEnumerable<User>> GetUsersService()
        {
            return await _dbContext.Users.ToListAsync();
        }

        public async Task<User?> UpdateUserService(int id, UpdateUserDTO userDTO)
        {
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (existingUser == null)
                throw new AppException("USER_NOT_FOUND", $"User with ID {id} not found", StatusCodes.Status404NotFound);

            if (existingUser.UserName != userDTO.UserName)
            {
                var usernameExists = await _dbContext.Users
                    .AnyAsync(u => u.UserName == userDTO.UserName && u.Id != id);
                if (usernameExists)
                    throw new AppException("USERNAME_EXISTS", $"Username '{userDTO.UserName}' is already taken", StatusCodes.Status409Conflict);
            }

            existingUser.FirstName = userDTO.FirstName;
            existingUser.MiddleName = userDTO.MiddleName;
            existingUser.LastName = userDTO.LastName;
            existingUser.Email = userDTO.Email;
            existingUser.UserName = userDTO.UserName;
            existingUser.PhoneNumber = userDTO.PhoneNumber;
            existingUser.Gender = userDTO.Gender;
            existingUser.ProfileImageUrl = userDTO.ProfileImageUrl;
            existingUser.LastUpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return existingUser;
        }

        public Task<UserResponseDTO?> GetCurrentUserService()
            => GetUserByIDService(_currentUser.UserId);

        public Task<User?> UpdateCurrentUserService(UpdateUserDTO userDTO)
            => UpdateUserService(_currentUser.UserId, userDTO);

        public async Task<User?> DeleteUserService(int id)
        {
            var existingUser = await _dbContext.Users.FindAsync(id);

            if (existingUser == null)
                throw new AppException("USER_NOT_FOUND", $"User with ID {id} not found", StatusCodes.Status404NotFound);

            _dbContext.Users.Remove(existingUser);
            await _dbContext.SaveChangesAsync();
            return existingUser;
        }
    }
}