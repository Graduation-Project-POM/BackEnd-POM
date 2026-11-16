namespace MIO2.Core.DTOs.GroupDTOs;

public record UpdateGroupDto(
    string Name,
    string? Description,
    // Admins only: reassigns the group to this trainer when set.
    int? TrainerId = null
);