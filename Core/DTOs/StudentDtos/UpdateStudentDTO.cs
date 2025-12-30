using System.ComponentModel.DataAnnotations;

namespace MIO.Core.DTOs.StudentDtos
{
    public class UpdateStudentDTO
    {
        [Required] 
        public string? GradeLevel { get; set; }
        [Required]
        public string? City { get; set; }
        [Required]
        [MinLength(2)]
        public string? SchoolName { get; set; }
        [Required]
        [MinLength(9)]
        [MaxLength(9)]
        public string? NationalId { get; set; }
        
    }
}
