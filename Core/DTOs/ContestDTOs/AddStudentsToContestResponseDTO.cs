namespace MIO.Core.DTOs.ContestDTOs;

public class AddStudentsToContestResponseDTO
{
    public string Message { get; set; } = string.Empty;
    public ICollection<int> MissingIds { get; set; } = [];
}