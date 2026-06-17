using System.ComponentModel.DataAnnotations;

namespace MIO2.Core.DTOs.GroupDTOs;

public record GroupStudentDto(
    [Required]
    int StudentId,
    [Required]
    string FullName,  
    [Required]
    DateOnly JoinDate
);