namespace MIO2.Core.DTOs.GroupDTOs;

public record GroupResponseDto(
    int Id,
    string Name,
    string? Description,
    DateOnly StartDate,
    int TrainerId
);