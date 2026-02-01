using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIO.Core.DTOs.TrainerDtos;
using MIO.Core.DTOs.UserDtos;
using MIO.Core.Entities;
using MIO.Core.Interfaces;
using MIO.Infrastructure.Services;
using System.Security.Claims;

namespace MIO.API.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    [Authorize]

    public class TrainerController(ITrainerService trainerService) : ControllerBase
    {
        [HttpPost]
        [Route("")]
        public async Task<ActionResult> createTrainer(CreateTrainerDTO trainerDTO)
        {
          
             var trainer= await trainerService.CreateTrainer(trainerDTO);

            return new OkObjectResult(new
            {
                message = "User Created successfully",
                userId = trainer.Id
            });

        }

        [HttpGet]
        [Route("")]

        public async Task<ActionResult<IEnumerable<TrainerResponseDTO>>> GetTrainers()
        {
            var records = await trainerService.GetTrainersService();
            return Ok(records);
        }


        [HttpGet]
        [Route("{id}")]

        public async Task<ActionResult> GetTrainerById(int id)
        {
            var user = await trainerService.GetTrainerByIDService(id);


            return Ok(user);
        }

        [HttpPut]
        [Route("{id}")]

        public async Task<ActionResult> UpdateTrainer(int id, UpdateTrainerDTO trainerDTO)
        {

            if (trainerDTO == null)
                return new BadRequestObjectResult("trainer data cannot be null");

            if (id <= 0)
                return new BadRequestObjectResult("Invalid trainer ID");

           
                var updatedTrainer= await trainerService.UpdateTrainerService(id, trainerDTO);
                
                if(updatedTrainer == null)
                    return NotFound($"Trtainer with ID {id} not found");

                return new OkObjectResult(new
                {
                    message = "Trainer updated successfully",
                    userId = updatedTrainer.Id
                });


           

        }

        [HttpDelete]
        [Route("{id}")]

        public async Task<ActionResult> DeleteTrainer(int id)
        {
            if (id <= 0)
                return new BadRequestObjectResult("Invalid trainer ID");

           
                var deletedTrainer = await trainerService.DeleteTrainerService(id);

            if (deletedTrainer == null)
                return NotFound();

            return new OkObjectResult(new
                {
                    message = "Trainer Deleted successfully",
                    userId = deletedTrainer.Id
                });
        }


    }
}
