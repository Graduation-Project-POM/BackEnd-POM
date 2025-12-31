using System.ComponentModel.DataAnnotations;

namespace MIO.Core.DTOs.TrainerDtos
{
    public class UpdateTrainerDTO
    {
        [Required]
        public string Specialization { get; set; }

    }
}
