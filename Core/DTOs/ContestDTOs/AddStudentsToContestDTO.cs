namespace MIO.Core.DTOs.ContestDTOs;

public class AddStudentsToContestDTO
{
    public ICollection<int> StudentIds { get; set; } = [];
}