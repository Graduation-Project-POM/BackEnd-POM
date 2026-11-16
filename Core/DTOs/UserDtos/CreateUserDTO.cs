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
            // Optional: a secure random password is generated when null or "".
            // RegularExpression skips empty values, so "" isn't rejected; any
            // password that is supplied still needs at least 8 characters.
            [RegularExpression(@"^[\s\S]{8,}$", ErrorMessage = "The field Password must be at least 8 characters.")]
             public string? Password { get; set; }
            [Required]
            [Range(1, 10)] 
             public required Role RoleID { get; set; }

             // Admin create only; defaults to active. Ignored by public sign-up.
             public bool? IsActive { get; set; }

             public DateTime CreatedAt { get; set; }
    }

    
}
