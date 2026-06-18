using MIO.Core.DTOs.StudentDTO;
using MIO.Core.DTOs.StudentDtos;
using MIO.Core.DTOs.TrainerDtos;
using MIO.Core.Entities;

namespace MIO.Core.DTOs.UserDtos
{
    public class UserResponseDTO
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public required string LastName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public required string Gender { get; set; }
        public string ProfileImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }


       public BasicTrainerDTO? BasicTrainer { get; set; }
        public BasicStudentDTO? BasicStudent { get; set; } 

    }
}
