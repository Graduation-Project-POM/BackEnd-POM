using MIO.Core.DTOs.StudentDTO;

namespace MIO2.Core.DTOs.StudentDtos;

public class MultipleStudentsResponseDTO
{
    public IEnumerable<StudentResponseDTO> Students { get; set; } = [];
    public IEnumerable<int> MissingIds { get; set; } = [];
}