using System;
namespace PukulTikus.Domain
{
    public class PlayerSave
    {
        public int Id { get; set; }

        // Unik per pemain (biar gampang upsert berdasarkan nama)
        public string PlayerName { get; set; } = "";

        // Snapshot gameplay
        public int Score { get; set; }
        public int Kills { get; set; }
        public int MaxCombo { get; set; }
        public int ValidHits { get; set; }
        public int MissClicks { get; set; }
        public int PunishmentHits { get; set; }

        public int Hearts { get; set; }             // sisa nyawa
        public int PhaseIndex { get; set; }         // fase aktif saat disimpan (0=F1,1=F2,2=F3)
        public int TimeLeftSec { get; set; }        // kalau masih pakai timer; kalau endless bisa 0

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
