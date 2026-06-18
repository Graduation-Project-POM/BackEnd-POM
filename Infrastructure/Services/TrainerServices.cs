using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MIO.API.Authentication;
using MIO.API.Settings;
using MIO.Core.DTOs.TrainerDtos;
using MIO.Core.DTOs.UserDtos;
using MIO.Core.Entities;
using MIO.Core.Enums;
using MIO.Core.Interfaces;
using MIO.Infrastructure.Services;
namespace MIO.Infrastructure.Services
{
    public class TrainerServices(AppDbContext _dbContext,ICurrentUserService _currentUser) : ITrainerService
    {

        public async Task<Trainer?> CreateTrainer(CreateTrainerDTO trainerDTO)
        {
        var user=await _dbContext.Users.FirstOrDefaultAsync(u=>u.Id==trainerDTO.userId);
            
            if(user is null)
            {
                throw new AppException("there is no user with the id you've enterd, please create a new user or use another user id"
                    ,StatusCodes.Status404NotFound);
            }
            
            if (await _dbContext.Trainers.AnyAsync(t => t.UserId == trainerDTO.userId))
            {
                throw new AppException("this user is already a trainer, choose another user id"
                    ,StatusCodes.Status409Conflict);
            }

                if (user.RoleID != Role.Trainer)
                {
                    throw new AppException("this user is not a trainer!"
                        ,StatusCodes.Status409Conflict);
                }
            
          
            

            var trainer = new Trainer 
            { 
            Specialization=trainerDTO.Specialization,
            UserId=trainerDTO.userId,
            
            };

            _dbContext.Trainers.Add(trainer);

            await _dbContext.SaveChangesAsync();

            return trainer;
        }

        public async Task<TrainerResponseDTO?> GetTrainerByIDService(int TrainerId)
        {
                var trainer = await _dbContext.Trainers
                                              .Include(t => t.User)
                                              .FirstOrDefaultAsync(t => t.Id == TrainerId);
                

            if (trainer == null)
                throw new AppException($"Trainer with ID {TrainerId} not found"
                   , StatusCodes.Status404NotFound);

            
            // Profile viewing is open to any authenticated user (View Profile
            // feature): a student must be able to see their group's trainer.
            
            
            return new TrainerResponseDTO
            {
                Id = trainer.Id,
                Specialization = trainer.Specialization,
                BasicUser = new BasicUserDTO
                {
                    Id = trainer.UserId,
                    FirstName = trainer.User.FirstName,
                    LastName = trainer.User.LastName,
                    Email = trainer.User.Email,
                    UserName = trainer.User.UserName,
                    PhoneNumber = trainer.User.PhoneNumber
                }
            };

        } 

        public async Task<IEnumerable<TrainerResponseDTO>> GetTrainersService()
        {
            var records =  await _dbContext.Trainers
                .Include(s => s.User)
                .ToListAsync();
            return records.Select(trainer => new TrainerResponseDTO
            {
                Id = trainer.Id,
                Specialization = trainer.Specialization,
                BasicUser = new BasicUserDTO
                {
                    Id = trainer.UserId,
                    FirstName = trainer.User.FirstName,
                    LastName = trainer.User.LastName,
                    Email = trainer.User.Email,
                    UserName = trainer.User.UserName,
                    PhoneNumber = trainer.User.PhoneNumber,
                }
            });
        }

        public async Task<Trainer?> UpdateTrainerService(int id, UpdateTrainerDTO trainerDTO)
        {
            var existingTrainer = await _dbContext.Trainers
                                                  .Include(t => t.User)
                                                     .FirstOrDefaultAsync(t => t.Id == id);

            if (existingTrainer == null)
            {
               
                throw new AppException($"Trainer with ID {id} not found"
                   , StatusCodes.Status404NotFound);

            }

            existingTrainer.Specialization = trainerDTO.Specialization;


            _dbContext.Set<Trainer>().Update(existingTrainer);
            await _dbContext.SaveChangesAsync();

            return existingTrainer;

           

        }

        private async Task<Trainer> GetOrCreateCurrentTrainer()
        {
            var userId = _currentUser.UserId;
            var trainer = await _dbContext.Trainers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (trainer == null)
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null || user.RoleID != Role.Trainer)
                    throw new AppException("Only trainers have a trainer profile", StatusCodes.Status403Forbidden);

                trainer = new Trainer { UserId = userId, Specialization = "" };
                _dbContext.Trainers.Add(trainer);
                await _dbContext.SaveChangesAsync();
                trainer.User = user;
            }

            return trainer;
        }

        public async Task<TrainerResponseDTO?> GetMyTrainerService()
        {
            var trainer = await GetOrCreateCurrentTrainer();
            return new TrainerResponseDTO
            {
                Id = trainer.Id,
                Specialization = trainer.Specialization,
                BasicUser = new BasicUserDTO
                {
                    Id = trainer.UserId,
                    FirstName = trainer.User.FirstName,
                    LastName = trainer.User.LastName,
                    Email = trainer.User.Email,
                    UserName = trainer.User.UserName,
                    PhoneNumber = trainer.User.PhoneNumber
                }
            };
        }

        public async Task<Trainer?> UpdateMyTrainerService(UpdateTrainerDTO trainerDTO)
        {
            var trainer = await GetOrCreateCurrentTrainer();
            trainer.Specialization = trainerDTO.Specialization;
            await _dbContext.SaveChangesAsync();
            return trainer;
        }

        public async Task<Trainer?> DeleteTrainerService(int id)
        {


            var existingTrainer = await _dbContext.Set<Trainer>().FindAsync(id);


            if (existingTrainer == null)
                throw new AppException($"Trainer with ID {id} not found"
                 , StatusCodes.Status404NotFound);


            _dbContext.Set<Trainer>().Remove(existingTrainer);

            await _dbContext.SaveChangesAsync();

            return existingTrainer;

        }

    }
}
