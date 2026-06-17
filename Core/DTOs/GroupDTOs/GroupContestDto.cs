using System.ComponentModel.DataAnnotations;

namespace MIO2.Core.DTOs.GroupDTOs;

public record GroupContestDto(
    [Required]
    int ContestId,
    string Title,
    DateOnly AddDate
);