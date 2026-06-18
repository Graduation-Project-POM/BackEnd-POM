using System.ComponentModel.DataAnnotations;

namespace MIO2.Core.DTOs.GroupDTOs;

public record GroupStudentDto(
    [Required]
    int StudentId,
    [Required]
    string FullName,
    [Required]
    DateOnly JoinDate,
    // Number of problems in this group's contests the student has solved
    // (submitted a solution for). Real, derived from ProblemStudents.
    [Required]
    int ProblemSolved
);