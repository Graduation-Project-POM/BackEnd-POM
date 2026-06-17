namespace MIO2.Core.DTOs.GroupDTOs;

public record AddStudentsToGroupDto(ICollection<int> StudentIds);

public record AddStudentsToGroupResponseDto(
    string Message,
    List<int> AlreadyInGroup,
    List<int> MissingIds
);  