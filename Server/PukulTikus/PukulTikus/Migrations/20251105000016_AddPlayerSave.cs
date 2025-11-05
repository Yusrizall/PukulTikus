using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PukulTikus.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerSave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerSaves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerName = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Kills = table.Column<int>(type: "INTEGER", nullable: false),
                    Combo = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxCombo = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidHits = table.Column<int>(type: "INTEGER", nullable: false),
                    MissClicks = table.Column<int>(type: "INTEGER", nullable: false),
                    PunishmentHits = table.Column<int>(type: "INTEGER", nullable: false),
                    HeartsCurrent = table.Column<int>(type: "INTEGER", nullable: false),
                    HeartsMax = table.Column<int>(type: "INTEGER", nullable: false),
                    PhaseIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSaves", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSaves_PlayerName",
                table: "PlayerSaves",
                column: "PlayerName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerSaves");
        }
    }
}
