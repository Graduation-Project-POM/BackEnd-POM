namespace MIO.Core.DTOs.ContestDTOs;

public class ContestLeaderboardEntryDTO
{
    public int Rank { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int Score { get; set; }
}
