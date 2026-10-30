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
            var userName = await ResolveUserName(userDTO);

            Security.CreatePasswordHash(userDTO.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User
            {
                FirstName = userDTO.FirstName,
                MiddleName = userDTO.MiddleName,
                LastName = userDTO.LastName,
                Email = userDTO.Email,
                UserName = userName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                PhoneNumber = userDTO.PhoneNumber,
                Gender = userDTO.Gender,
                ProfileImageUrl = userDTO.ProfileImageUrl,
                IsActive = userDTO.IsActive ?? true,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                RoleID = userDTO.RoleID
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            await EnsureRoleProfile(user);
            return user;
        }

        // Use the supplied username if any (public sign-up); otherwise generate
        // a unique one from the user's name, e.g. "john.smith", "john.smith2".
        private async Task<string> ResolveUserName(CreateUserDTO userDTO)
        {
            if (await _dbContext.Users.AnyAsync(u => u.Email == userDTO.Email))
                throw new AppException("EMAIL_EXISTS", "Email is already in use", StatusCodes.Status409Conflict);

            if (!string.IsNullOrWhiteSpace(userDTO.UserName))
            {
                var requested = userDTO.UserName.Trim();
                if (await _dbContext.Users.AnyAsync(u => u.UserName == requested))
                    throw new AppException("USERNAME_EXISTS", "Username already exists", StatusCodes.Status409Conflict);
                return requested;
            }

            static string Clean(string? s) =>
                new string((s ?? "").Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

            var first = Clean(userDTO.FirstName);
            var last = Clean(userDTO.LastName);
            var baseName = string.Join(".", new[] { first, last }.Where(p => p.Length > 0));
            if (baseName.Length < 4)
                baseName = Clean(userDTO.Email?.Split('@')[0]);
            if (baseName.Length < 4)
                baseName = "user" + baseName;
            if (baseName.Length > 56)
                baseName = baseName[..56];

            var taken = await _dbContext.Users
                .Where(u => u.UserName.StartsWith(baseName))
                .Select(u => u.UserName)
                .ToListAsync();
            var takenSet = new HashSet<string>(taken, StringComparer.OrdinalIgnoreCase);

            var candidate = baseName;
            for (var i = 2; takenSet.Contains(candidate); i++)
                candidate = baseName + i;
            return candidate;
        }

        // Ensure a user with the Student/Trainer role has the matching entity row,
        // so they appear in GET /Student and GET /Trainer (and can join groups).
        // studentAccepted: admin-created students are accepted immediately;
        // public self-registrations start pending so an admin approves them.
        private async Task EnsureRoleProfile(User user, bool studentAccepted = true)
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
                    IsAccepted = studentAccepted
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
            var userName = await ResolveUserName(userDTO);

            Security.CreatePasswordHash(userDTO.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User
            {
                FirstName = userDTO.FirstName,
                MiddleName = userDTO.MiddleName,
                LastName = userDTO.LastName,
                Email = userDTO.Email,
                UserName = userName,
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
            // Public registration → pending until an admin approves on the Approvals page.
            await EnsureRoleProfile(user, studentAccepted: false);
            return user;
        }

        public async Task<User?> GetUserService(AuthenticationRequest request)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);

            if (user == null)
                throw new AppException("INVALID_CREDENTIALS", "Invalid username or password", StatusCodes.Status401Unauthorized);

            if (!Security.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
                throw new AppException("INVALID_CREDENTIALS", "Invalid username or password", StatusCodes.Status401Unauthorized);

            // Block login for students whose account is still pending admin approval.
            if (user.RoleID == Role.Student)
            {
                var student = await _dbContext.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student != null && !student.IsAccepted)
                    throw new AppException(
                        "ACCOUNT_PENDING",
                        "Your account is pending admin approval",
                        StatusCodes.Status403Forbidden);
            }

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
                Bio = user.Bio,
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

        public async Task<IEnumerable<UserResponseDTO>> GetUsersService()
        {

            return await _dbContext.Users
                .OrderByDescending(user => user.CreatedAt)
                .ThenByDescending(user => user.Id)
                .Select(user => new UserResponseDTO
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
                    Bio = user.Bio,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastUpdatedAt = user.LastUpdatedAt,
                    BasicTrainer = user.Trainer != null
                        ? new BasicTrainerDTO
                        {
                            Id = user.Trainer.Id,
                            Specialization = user.Trainer.Specialization
                        }
                        : null,
                    BasicStudent = user.Student != null
                        ? new BasicStudentDTO
                        {
                            Id = user.Student.Id,
                            City = user.Student.City,
                            GradeLevel = user.Student.GradeLevel,
                            JoinYear = user.Student.JoinYear,
                            NationalId = user.Student.NationalId,
                            ProblemSolved = user.Student.ProblemSolved,
                            SchoolName = user.Student.SchoolName,
                            Score = user.Student.Score
                        }
                        : null
                })
                .ToListAsync();
        }

        public async Task<User?> UpdateUserService(int id, AdminUpdateUserDTO userDTO)
        {
            var existingUser = await ApplyProfileUpdate(id, userDTO);

            var roleChanged = userDTO.RoleID.HasValue && userDTO.RoleID.Value != existingUser.RoleID;
            var deactivated = userDTO.IsActive == false && existingUser.IsActive;
            if (id == _currentUser.UserId && (roleChanged || deactivated))
                throw new AppException("CANNOT_CHANGE_OWN_ACCESS", "You can't change your own role or deactivate your own account", StatusCodes.Status400BadRequest);

            if (userDTO.RoleID.HasValue)
                existingUser.RoleID = userDTO.RoleID.Value;
            if (userDTO.IsActive.HasValue)
                existingUser.IsActive = userDTO.IsActive.Value;

            var passwordChanged = !string.IsNullOrEmpty(userDTO.NewPassword);
            if (passwordChanged)
            {
                Security.CreatePasswordHash(userDTO.NewPassword!, out byte[] passwordHash, out byte[] passwordSalt);
                existingUser.PasswordHash = passwordHash;
                existingUser.PasswordSalt = passwordSalt;
            }

            // Force a fresh login so the new password / role / status takes effect.
            if (passwordChanged || roleChanged || deactivated)
                existingUser.RefreshTokenRevokedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            await EnsureRoleProfile(existingUser);
            return existingUser;
        }

        // Profile fields shared by the admin edit and PUT /User/me. Not saved yet.
        private async Task<User> ApplyProfileUpdate(int id, UpdateUserDTO userDTO)
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

            if (existingUser.Email != userDTO.Email &&
                await _dbContext.Users.AnyAsync(u => u.Email == userDTO.Email && u.Id != id))
                throw new AppException("EMAIL_EXISTS", $"Email '{userDTO.Email}' is already in use", StatusCodes.Status409Conflict);

            existingUser.FirstName = userDTO.FirstName;
            existingUser.MiddleName = userDTO.MiddleName;
            existingUser.LastName = userDTO.LastName;
            existingUser.Email = userDTO.Email;
            existingUser.UserName = userDTO.UserName;
            existingUser.PhoneNumber = userDTO.PhoneNumber;
            existingUser.Gender = userDTO.Gender;
            existingUser.ProfileImageUrl = userDTO.ProfileImageUrl;
            // The admin form has no bio field; null means "keep", "" clears it.
            if (userDTO.Bio != null)
                existingUser.Bio = userDTO.Bio;
            existingUser.LastUpdatedAt = DateTime.UtcNow;

            return existingUser;
        }

        public Task<UserResponseDTO?> GetCurrentUserService()
            => GetUserByIDService(_currentUser.UserId);

        public async Task<User?> UpdateCurrentUserService(UpdateUserDTO userDTO)
        {
            var user = await ApplyProfileUpdate(_currentUser.UserId, userDTO);
            await _dbContext.SaveChangesAsync();
            return user;
        }

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