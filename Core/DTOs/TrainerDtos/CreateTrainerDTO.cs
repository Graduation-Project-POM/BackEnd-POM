using System.ComponentModel.DataAnnotations;
using MIO.Core.Entities;
using MIO.Infrastructure.Services;
namespace MIO.Core.DTOs.TrainerDtos
{
    public class CreateTrainerDTO
    {
        [Required]
        public string Specialization { get; set; }

        [Required]
        public int userId { get; set; }

        public User User { get; set; }
        
       
    }
}
