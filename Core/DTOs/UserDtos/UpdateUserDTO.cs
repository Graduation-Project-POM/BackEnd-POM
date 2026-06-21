using System.ComponentModel.DataAnnotations;
using MIO.Core.Enums;

namespace MIO.Core.DTOs.UserDtos
{
    public class UpdateUserDTO
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
            [Required]
            [MinLength(4)]  
            public required string UserName { get; set; }
            [Phone]
            public string? PhoneNumber { get; set; }
            [Required]
            public Gender? Gender { get; set; }

            public string ProfileImageUrl { get; set; }

            public string? Bio { get; set; }

            public DateTime LastUpdatedAt { get; set; }
    }
}
