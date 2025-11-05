using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PukulTikus.Domain;

namespace PukulTikus.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

        public DbSet<PlayerScore> PlayerScores => Set<PlayerScore>();
        public DbSet<PlayerSave> PlayerSaves => Set<PlayerSave>(); // <- BARU

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // converter DateTimeOffset <-> INTEGER (epoch detik) untuk SQLite
            var dtoToLong = new ValueConverter<DateTimeOffset, long>(
                v => v.ToUnixTimeSeconds(),
                v => DateTimeOffset.FromUnixTimeSeconds(v)
            );

            // ===== PlayerScore (sudah ada) =====
            var s = modelBuilder.Entity<PlayerScore>();
            s.Property(p => p.PlayerName).IsRequired().HasMaxLength(20);
            s.Property(p => p.Score).IsRequired();
            s.Property(p => p.Kills).IsRequired();
            s.Property(p => p.MaxCombo).IsRequired();
            s.Property(p => p.Accuracy).IsRequired();
            s.Property(p => p.DurationSec).HasDefaultValue(60);
            s.Property(p => p.ValidHits).IsRequired();
            s.Property(p => p.MissClicks).IsRequired();
            s.Property(p => p.PunishmentHits).IsRequired();
            s.Property(p => p.CreatedAt).HasConversion(dtoToLong).IsRequired();
            s.HasIndex(p => new { p.Score, p.Kills, p.MaxCombo, p.CreatedAt });

            // ===== PlayerSave (BARU) =====
            var e = modelBuilder.Entity<PlayerSave>();
            e.Property(p => p.PlayerName).IsRequired().HasMaxLength(20);
            e.HasIndex(p => p.PlayerName).IsUnique(); // satu save per nama

            e.Property(p => p.CreatedAt).HasConversion(dtoToLong).IsRequired();
            e.Property(p => p.UpdatedAt).HasConversion(dtoToLong).IsRequired();
        }
    }
}
