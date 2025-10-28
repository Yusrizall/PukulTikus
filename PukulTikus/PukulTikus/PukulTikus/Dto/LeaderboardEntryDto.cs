namespace PukulTikus.Dto;

public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public int Id { get; set; }
    public string PlayerName { get; set; } = "";
    public int Score { get; set; }
    public int Kills { get; set; }
    public int MaxCombo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
