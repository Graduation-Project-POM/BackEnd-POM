using MIO2.Core.Entities;

namespace MIO.Core.DTOs.ContestDTOs;

public class ContestsStudentsResponseDTO
{
    public ICollection<ContestStudent> ContestStudents { get; set; } = [];
}