using MIO.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace MIO.Core.DTOs.UserDtos
{
    public class CreateUserDTO
    {
            [Required]
            [MinLength(2)] 
            public required string FirstName { get; set; }

            public string? MiddleName { get; set; }
            [Required]
            [MinLength(2)]
             public required string LastName { get; set; }
            [Required]
            [EmailAddress]
            public string Email { get; set; }
            // Optional: generated from the user's name when left empty.
            [MinLength(4)]
            public string? UserName { get; set; }
            [Phone]
            public string? PhoneNumber { get; set; }
            public Gender? Gender { get; set; }

            public string ProfileImageUrl { get; set; }
            [Required]
            [MinLength(8)]
             public required string Password { get; set; }
            [Required]
            [Range(1, 10)] 
             public required Role RoleID { get; set; }

             public DateTime CreatedAt { get; set; }
    }

    
}
