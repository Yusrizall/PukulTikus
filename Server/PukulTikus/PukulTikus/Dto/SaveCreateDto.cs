namespace PukulTikus.Dto
{
    public class SaveCreateDto
    {
        public string PlayerName { get; set; } = string.Empty;

        public int Score { get; set; }
        public int Kills { get; set; }
        public int Combo { get; set; }
        public int MaxCombo { get; set; }
        public int ValidHits { get; set; }
        public int MissClicks { get; set; }
        public int PunishmentHits { get; set; }

        public int HeartsCurrent { get; set; }
        public int HeartsMax { get; set; }

        public int PhaseIndex { get; set; }
    }
}
