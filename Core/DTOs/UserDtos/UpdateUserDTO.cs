using System.ComponentModel.DataAnnotations;

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
            [Required]
            [Phone]
            public required string PhoneNumber { get; set; }
            [Required]
            [RegularExpression("male|female", ErrorMessage = "Gender must be male or female")]
            public required string Gender { get; set; }

            public string ProfileImageUrl { get; set; }
            
            public DateTime LastUpdatedAt { get; set; }
    }
}
