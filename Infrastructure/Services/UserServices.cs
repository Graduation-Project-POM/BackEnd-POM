using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MIO.API.Authentication;
using MIO.API.Settings;
using MIO.Core.DTOs;
using MIO.Core.DTOs.StudentDTO;
using MIO.Core.DTOs.StudentDtos;
using MIO.Core.DTOs.TrainerDtos;
using MIO.Core.DTOs.UserDtos;
using MIO.Core.Entities;
using MIO.Core.Enums;
using MIO.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace MIO.Infrastructure.Services
{
    public class UserServices : IUserService
    {
        private readonly AppDbContext _dbContext;
       private readonly ICurrentUserService _currentUser;

     

        public UserServices(AppDbContext dbContext ,ICurrentUserService currentUser)
        {
            _dbContext = dbContext;
            _currentUser = currentUser;
        }
        
        
       
        

        public async Task<User?> CreateUser(CreateUserDTO userDTO)
        {
            if( await _dbContext.Users.AnyAsync(u=>u.UserName==userDTO.UserName))
            {
                throw new AppException("Username already exists",
                    StatusCodes.Status409Conflict);  
            }

            Security.CreatePasswordHash(userDTO.Password, out byte[] PasswordHash, out byte[] Passwordsalt);


            var user = new User
            {
                FirstName = userDTO.FirstName,
                MiddleName = userDTO.MiddleName,
                LastName = userDTO.LastName,
                Email = userDTO.Email,
                UserName = userDTO.UserName,
                PasswordHash= PasswordHash,
                PasswordSalt= Passwordsalt,
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

            return user; 

        }

        public async Task<User?> RegisterStudent(CreateUserDTO userDTO)
        {
            if (await _dbContext.Users.AnyAsync(u => u.UserName == userDTO.UserName))
                throw new AppException("Username already exists", StatusCodes.Status409Conflict);

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
            return user;
        }

        public async Task<User?> GetUserService(AuthenticationRequest request)
        {
            
            var user=await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);

            if (user == null)
                throw new AppException($"User not found"
                 , StatusCodes.Status404NotFound);

            if (!Security.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
                throw new AppException($"Invalid username or password"
                 , StatusCodes.Status401Unauthorized);

            return user;

        }

        public async Task<UserResponseDTO?> GetUserByIDService(int UserId)
        {
           

            var user = await _dbContext.Users
                                       .Include(u=>u.Trainer)
                                       .Include(u=>u.Student)
                                       .FirstOrDefaultAsync(u => u.Id==UserId);

            if (user == null)
                throw new AppException($"User with ID {UserId} not found"
                  , StatusCodes.Status404NotFound);

            var basicTrainer = new BasicTrainerDTO();
            var basictudent=new BasicStudentDTO();

            if (user.Trainer!=null)
            {
                basicTrainer = new BasicTrainerDTO {
                Id=user.Trainer.Id,
                Specialization=user.Trainer.Specialization
                };
            }

            if (user.Student != null)
            {
                basictudent = new BasicStudentDTO
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
            }


            var  userResponse=new UserResponseDTO
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

            if (basicTrainer is not null)
                userResponse.BasicTrainer = basicTrainer;

            if (basictudent is not null)
                userResponse.BasicStudent = basictudent;


            return userResponse;

        }
       
        public async Task<IEnumerable<User>> GetUsersService()
        {
            var records = await _dbContext.Set<User>().ToListAsync();
            return records;
        }

        public async Task<User?> UpdateUserService(int id,UpdateUserDTO userDTO)
        {

            var existingUser = await _dbContext.Users
                                               .FirstOrDefaultAsync(u => u.Id == id);
            

            if (existingUser == null)
            {
              throw new AppException($"User with ID {id} not found"
                  ,StatusCodes.Status404NotFound);

            }

            if (existingUser.UserName != userDTO.UserName)
            {
                var usernameExists = await _dbContext.Users
                    .AnyAsync(u => u.UserName == userDTO.UserName && u.Id != id);
                if (usernameExists)
                    throw new AppException($"Username '{userDTO.UserName}' is already taken"
                        ,StatusCodes.Status409Conflict);
            }


            existingUser.FirstName = userDTO.FirstName;
            existingUser.MiddleName = userDTO.MiddleName;
            existingUser.LastName = userDTO.LastName;
            existingUser.Email = userDTO.Email;
            existingUser.UserName = userDTO.UserName;   
            existingUser.PhoneNumber= userDTO.PhoneNumber;
            existingUser.Gender = userDTO.Gender;
            existingUser.ProfileImageUrl = userDTO.ProfileImageUrl;
            existingUser.LastUpdatedAt=DateTime.Now;
            
            _dbContext.Set<User>().Update(existingUser);
            await _dbContext.SaveChangesAsync();

            return existingUser;
           

        }

        public async Task<User?> DeleteUserService(int id)
        {
            var existingUser = await _dbContext.Set<User>().FindAsync(id);

            if(existingUser == null)
                throw new AppException($"User with ID {id} not exist"
                    ,StatusCodes.Status404NotFound);

            _dbContext.Set<User>().Remove(existingUser);

            await _dbContext.SaveChangesAsync();
            return existingUser;

           
        }


    }
}
