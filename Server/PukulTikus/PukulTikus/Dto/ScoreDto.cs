namespace PukulTikus.Dto;

public class ScoreDto
{
    public int Id { get; set; }
    public string PlayerName { get; set; } = "";
    public int Score { get; set; }
    public int Kills { get; set; }
    public int MaxCombo { get; set; }
    public double Accuracy { get; set; }
    public int DurationSec { get; set; }
    public int ValidHits { get; set; }
    public int MissClicks { get; set; }
    public int PunishmentHits { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
