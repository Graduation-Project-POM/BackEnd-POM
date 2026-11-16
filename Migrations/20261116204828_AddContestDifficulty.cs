using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIO.Migrations
{
    /// <inheritdoc />
    public partial class AddContestDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "Contests",
                type: "int",
                nullable: false,
                // Existing contests become Medium (Difficulty.Medium = 2); 0 is not a valid difficulty.
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Contests");
        }
    }
}
