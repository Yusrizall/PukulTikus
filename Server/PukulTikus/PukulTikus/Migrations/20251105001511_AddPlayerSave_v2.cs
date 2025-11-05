using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PukulTikus.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerSave_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Combo",
                table: "PlayerSaves");

            migrationBuilder.RenameColumn(
                name: "HeartsMax",
                table: "PlayerSaves",
                newName: "TimeLeftSec");

            migrationBuilder.RenameColumn(
                name: "HeartsCurrent",
                table: "PlayerSaves",
                newName: "Hearts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeLeftSec",
                table: "PlayerSaves",
                newName: "HeartsMax");

            migrationBuilder.RenameColumn(
                name: "Hearts",
                table: "PlayerSaves",
                newName: "HeartsCurrent");

            migrationBuilder.AddColumn<int>(
                name: "Combo",
                table: "PlayerSaves",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
