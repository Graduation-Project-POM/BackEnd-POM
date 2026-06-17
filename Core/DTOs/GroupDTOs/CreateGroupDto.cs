using System.ComponentModel.DataAnnotations;

namespace MIO2.Core.DTOs.GroupDTOs;

public record CreateGroupDto(
    [Required]
    string Name,
    string? Description,
    int TrainerId
);