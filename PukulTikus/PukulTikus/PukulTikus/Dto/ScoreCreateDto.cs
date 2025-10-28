namespace PukulTikus.Dto;

public class ScoreCreateDto
{
    public string PlayerName { get; set; } = "";
    public int Score { get; set; }
    public int Kills { get; set; }
    public int MaxCombo { get; set; }
    public int DurationSec { get; set; } = 60;

    public int ValidHits { get; set; }
    public int MissClicks { get; set; }
    public int PunishmentHits { get; set; }
}
