namespace PukulTikus.Dto
{
    // Request dari Unity ketika menekan "Save & Back"
    public class SaveSnapshotDto
    {
        public string PlayerName { get; set; } = "";

        public int Score { get; set; }
        public int Kills { get; set; }
        public int MaxCombo { get; set; }
        public int ValidHits { get; set; }
        public int MissClicks { get; set; }
        public int PunishmentHits { get; set; }

        public int Hearts { get; set; }
        public int PhaseIndex { get; set; }
        public int TimeLeftSec { get; set; }
    }

    // Respon ke Unity
    public class SaveResponseDto
    {
        public int Id { get; set; }
        public string PlayerName { get; set; } = "";
        public int Score { get; set; }
        public int Kills { get; set; }
        public int MaxCombo { get; set; }
        public int ValidHits { get; set; }
        public int MissClicks { get; set; }
        public int PunishmentHits { get; set; }
        public int Hearts { get; set; }
        public int PhaseIndex { get; set; }
        public int TimeLeftSec { get; set; }
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
    }
}
