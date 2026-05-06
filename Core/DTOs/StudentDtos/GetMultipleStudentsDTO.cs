namespace MIO2.Core.DTOs.StudentDtos;

public class GetMultipleStudentsDTO
{
    public IEnumerable<int> Ids { get; set; } = [];
}