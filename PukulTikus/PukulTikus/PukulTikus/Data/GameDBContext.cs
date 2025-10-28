using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PukulTikus.Domain;

namespace PukulTikus.Data;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    public DbSet<PlayerScore> PlayerScores => Set<PlayerScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1) Buat converter: DateTimeOffset <-> long (epoch detik)
        var createdAtConverter = new ValueConverter<DateTimeOffset, long>(
            v => v.ToUnixTimeSeconds(),                // tulis ke DB (INTEGER)
            v => DateTimeOffset.FromUnixTimeSeconds(v) // baca dari DB -> DateTimeOffset (UTC)
        );

        // 2) Konfigurasi entity
        var e = modelBuilder.Entity<PlayerScore>();

        e.Property(p => p.PlayerName).IsRequired().HasMaxLength(20);
        e.Property(p => p.Score).IsRequired();
        e.Property(p => p.Kills).IsRequired();
        e.Property(p => p.MaxCombo).IsRequired();
        e.Property(p => p.Accuracy).IsRequired();
        e.Property(p => p.DurationSec).HasDefaultValue(60);
        e.Property(p => p.ValidHits).IsRequired();
        e.Property(p => p.MissClicks).IsRequired();
        e.Property(p => p.PunishmentHits).IsRequired();

        // 3) APPLY converter ke kolom CreatedAt
        e.Property(p => p.CreatedAt)
         .HasConversion(createdAtConverter)
         .IsRequired();

        // 4) Index leaderboard (tetap)
        e.HasIndex(p => new { p.Score, p.Kills, p.MaxCombo, p.CreatedAt });
    }
}
