namespace PukulTikus.Domain;

public class PlayerScore
{
    public int Id { get; set; }

    // wajib, max 20 (divalidasi di controller)
    public string PlayerName { get; set; } = "";

    // ≥ 0
    public int Score { get; set; }
    public int Kills { get; set; }
    public int MaxCombo { get; set; }

    // 0..1 (dihitung server)
    public double Accuracy { get; set; }

    public int DurationSec { get; set; } = 60;

    // konsistensi: ValidHits >= Kills
    public int ValidHits { get; set; }
    public int MissClicks { get; set; }
    public int PunishmentHits { get; set; }

    // server-set (UTC)
    public DateTimeOffset CreatedAt { get; set; }
}
