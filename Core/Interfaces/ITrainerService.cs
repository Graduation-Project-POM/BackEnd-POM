using Microsoft.AspNetCore.Mvc;
using MIO.Core.DTOs.TrainerDtos;
using MIO.Core.Entities;

namespace MIO.Core.Interfaces
{
    public interface ITrainerService
    {
       Task CreateTrainer(CreateTrainerDTO trainerDTO);

       Task<TrainerResponseDTO?> GetTrainerByIDService(int TrainerId);

       Task<IEnumerable<TrainerResponseDTO>> GetTrainersService();

        Task<Trainer?> UpdateTrainerService(int id, UpdateTrainerDTO trainerDTO);

       Task<Trainer?> DeleteTrainerService(int id);
    }
}
