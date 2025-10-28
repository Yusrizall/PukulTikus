using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PukulTikus.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerName = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Kills = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxCombo = table.Column<int>(type: "INTEGER", nullable: false),
                    Accuracy = table.Column<double>(type: "REAL", nullable: false),
                    DurationSec = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 60),
                    ValidHits = table.Column<int>(type: "INTEGER", nullable: false),
                    MissClicks = table.Column<int>(type: "INTEGER", nullable: false),
                    PunishmentHits = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerScores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerScores_Score_Kills_MaxCombo_CreatedAt",
                table: "PlayerScores",
                columns: new[] { "Score", "Kills", "MaxCombo", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerScores");
        }
    }
}
