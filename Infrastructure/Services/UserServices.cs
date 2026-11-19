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

        public async Task<CreatedUserResult> CreateUser(CreateUserDTO userDTO)
        {
            var userName = await ResolveUserName(userDTO);
            var phoneNumber = await ResolvePhoneNumber(userDTO.PhoneNumber);
            var (password, generatedPassword) = ResolvePassword(userDTO);

            Security.CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User
            {
                FirstName = userDTO.FirstName,
                MiddleName = userDTO.MiddleName,
                LastName = userDTO.LastName,
                Email = userDTO.Email,
                UserName = userName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                PhoneNumber = phoneNumber,
                Gender = userDTO.Gender,
                ProfileImageUrl = userDTO.ProfileImageUrl,
                IsActive = userDTO.IsActive ?? true,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                RoleID = userDTO.RoleID
            };

            _dbContext.Users.Add(user);
            await SaveUserChangesAsync();
            await EnsureRoleProfile(user);
            return new CreatedUserResult { User = user, GeneratedPassword = generatedPassword };
        }

        // Password for a new account: the one supplied, or a secure random one
        // when it was left empty. The generated value is returned so it can be
        // shown to whoever created the account; only its hash is stored.
        private static (string Password, string? Generated) ResolvePassword(CreateUserDTO userDTO)
        {
            if (!string.IsNullOrEmpty(userDTO.Password))
                return (userDTO.Password, null);

            var generated = Security.GenerateRandomPassword();
            return (generated, generated);
        }

        // Normalized phone number, or null when none was given. Every account's
        // phone number must be unique; exceptUserId skips the account being edited.
        private async Task<string?> ResolvePhoneNumber(string? phoneNumber, int? exceptUserId = null)
        {
            var normalized = PhoneNumbers.Normalize(phoneNumber);
            if (normalized != null &&
                await _dbContext.Users.AnyAsync(u => u.PhoneNumber == normalized && u.Id != exceptUserId))
                throw PhoneNumberInUse();
            return normalized;
        }

        private static AppException PhoneNumberInUse() =>
            new("PHONE_EXISTS", "This phone number is already registered to another account", StatusCodes.Status409Conflict);

        // SaveChanges for writes that set a phone number. The pre-check above
        // can't stop two simultaneous requests for the same number; the unique
        // index does, and this turns that rejection into the same friendly error.
        private async Task SaveUserChangesAsync()
        {
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (PhoneNumbers.IsDuplicatePhone(ex))
            {
                throw PhoneNumberInUse();
            }
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
        // accepted: admin-created accounts are accepted immediately; public
        // self-registrations start pending so an admin approves them.
        private async Task EnsureRoleProfile(User user, bool accepted = true)
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
                    IsAccepted = accepted
                });
                await _dbContext.SaveChangesAsync();
            }
            else if (user.RoleID == Role.Trainer &&
                !await _dbContext.Trainers.AnyAsync(t => t.UserId == user.Id))
            {
                _dbContext.Trainers.Add(new Trainer
                {
                    UserId = user.Id,
                    Specialization = "",
                    IsAccepted = accepted
                });
                await _dbContext.SaveChangesAsync();
            }
        }

        // Public sign-up is for students only. Trainer and admin accounts are
        // created by an admin, so the public form can never request those roles.
        public async Task<CreatedUserResult> RegisterUser(CreateUserDTO userDTO)
        {
            if (userDTO.RoleID != Role.Student)
                throw new AppException("INVALID_ROLE", "Only students can sign up", StatusCodes.Status400BadRequest);

            var userName = await ResolveUserName(userDTO);
            var phoneNumber = await ResolvePhoneNumber(userDTO.PhoneNumber);
            var (password, generatedPassword) = ResolvePassword(userDTO);

            Security.CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User
            {
                FirstName = userDTO.FirstName,
                MiddleName = userDTO.MiddleName,
                LastName = userDTO.LastName,
                Email = userDTO.Email,
                UserName = userName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                PhoneNumber = phoneNumber,
                Gender = userDTO.Gender,
                ProfileImageUrl = userDTO.ProfileImageUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                RoleID = userDTO.RoleID
            };

            _dbContext.Users.Add(user);
            await SaveUserChangesAsync();
            // Public registration → pending until an admin approves on the Approvals page.
            await EnsureRoleProfile(user, accepted: false);
            return new CreatedUserResult { User = user, GeneratedPassword = generatedPassword };
        }

        public async Task<User?> GetUserService(AuthenticationRequest request)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);

            if (user == null)
                throw new AppException("INVALID_CREDENTIALS", "Invalid username or password", StatusCodes.Status401Unauthorized);

            if (!Security.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
                throw new AppException("INVALID_CREDENTIALS", "Invalid username or password", StatusCodes.Status401Unauthorized);

            // Block login for accounts still pending admin approval, whatever the role.
            var pending = user.RoleID switch
            {
                Role.Student => await _dbContext.Students.AnyAsync(s => s.UserId == user.Id && !s.IsAccepted),
                Role.Trainer => await _dbContext.Trainers.AnyAsync(t => t.UserId == user.Id && !t.IsAccepted),
                _ => false
            };
            if (pending)
                throw new AppException(
                    "ACCOUNT_PENDING",
                    "Your account is pending admin approval",
                    StatusCodes.Status403Forbidden);

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

        // Student sign-up requests with their approval state, newest first. Each row
        // carries the role-profile id (used by the accept endpoints and profile
        // pages) and the user id (used by reject, which deletes the user).
        public async Task<IEnumerable<ApprovalResponseDTO>> GetApprovalsService()
        {
            var students = await _dbContext.Students
                .Select(s => new ApprovalResponseDTO
                {
                    UserId = s.UserId,
                    ProfileId = s.Id,
                    Role = "Student",
                    FirstName = s.User.FirstName,
                    LastName = s.User.LastName,
                    Email = s.User.Email,
                    PhoneNumber = s.User.PhoneNumber,
                    IsAccepted = s.IsAccepted,
                    CreatedAt = s.User.CreatedAt,
                    GradeLevel = s.GradeLevel,
                })
                .ToListAsync();

            // Trainers are created by an admin (already accepted), so only list
            // any still-pending trainer requests left from older self sign-ups.
            var trainers = await _dbContext.Trainers
                .Where(t => !t.IsAccepted)
                .Select(t => new ApprovalResponseDTO
                {
                    UserId = t.UserId,
                    ProfileId = t.Id,
                    Role = "Trainer",
                    FirstName = t.User.FirstName,
                    LastName = t.User.LastName,
                    Email = t.User.Email,
                    PhoneNumber = t.User.PhoneNumber,
                    IsAccepted = t.IsAccepted,
                    CreatedAt = t.User.CreatedAt,
                    Specialization = t.Specialization,
                })
                .ToListAsync();

            return students.Concat(trainers)
                .OrderByDescending(a => a.CreatedAt)
                .ThenByDescending(a => a.UserId)
                .ToList();
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

            await SaveUserChangesAsync();
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
            existingUser.PhoneNumber = await ResolvePhoneNumber(userDTO.PhoneNumber, id);
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
            await SaveUserChangesAsync();
            return user;
        }

        public async Task<User?> DeleteUserService(int id)
        {
            var existingUser = await _dbContext.Users.FindAsync(id);

            if (existingUser == null)
                throw new AppException("USER_NOT_FOUND", $"User with ID {id} not found", StatusCodes.Status404NotFound);

            if (id == _currentUser.UserId)
                throw new AppException("CANNOT_DELETE_SELF", "You can't delete your own account", StatusCodes.Status400BadRequest);

            // Users, Trainers, Students, comments and gallery rows use NO ACTION
            // foreign keys, so dependents must be cleared first, all-or-nothing.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            // Trainer: keep the problems/groups/contests they created, just unowned.
            var trainerIds = _dbContext.Trainers.Where(t => t.UserId == id).Select(t => (int?)t.Id);
            await _dbContext.Problems.Where(p => trainerIds.Contains(p.TrainerID))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.TrainerID, (int?)null));
            await _dbContext.Groups.Where(g => trainerIds.Contains(g.TrainerId))
                .ExecuteUpdateAsync(s => s.SetProperty(g => g.TrainerId, (int?)null));
            await _dbContext.Contests.Where(c => trainerIds.Contains(c.TrainerId))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.TrainerId, (int?)null));
            await _dbContext.Trainers.Where(t => t.UserId == id).ExecuteDeleteAsync();

            // Student: contest/group/problem rows cascade in the database;
            // bank submissions don't.
            var studentIds = _dbContext.Students.Where(st => st.UserId == id).Select(st => st.Id);
            await _dbContext.ProblemBankSubmissions.Where(b => studentIds.Contains(b.StudentId)).ExecuteDeleteAsync();
            await _dbContext.Students.Where(st => st.UserId == id).ExecuteDeleteAsync();

            // Same rule as deleting a discussion comment: replies go with their
            // top-level comment (replies are only ever one level deep).
            var commentIds = _dbContext.ProblemComments.Where(c => c.UserId == id).Select(c => (int?)c.Id);
            await _dbContext.ProblemComments.Where(c => commentIds.Contains(c.ParentCommentId)).ExecuteDeleteAsync();
            await _dbContext.ProblemComments.Where(c => c.UserId == id).ExecuteDeleteAsync();

            await _dbContext.Gallery.Where(g => g.UploadedByID == id).ExecuteDeleteAsync();

            await _dbContext.Users.Where(u => u.Id == id).ExecuteDeleteAsync();
            await transaction.CommitAsync();

            // The bulk deletes bypass the change tracker; drop any stale copies
            // (callers like DeleteStudentService load the profile first) so a
            // later SaveChanges on this context doesn't act on deleted rows.
            foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
            {
                if (entry.Entity is User u && u.Id == id ||
                    entry.Entity is Student st && st.UserId == id ||
                    entry.Entity is Trainer t && t.UserId == id)
                    entry.State = EntityState.Detached;
            }
            return existingUser;
        }
    }
}