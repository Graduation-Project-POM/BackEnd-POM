using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIO.Migrations
{
    /// <inheritdoc />
    public partial class Gallery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Gallery",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Gallery_UserId",
                table: "Gallery",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Gallery_Users_UserId",
                table: "Gallery",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Gallery_Users_UserId",
                table: "Gallery");

            migrationBuilder.DropIndex(
                name: "IX_Gallery_UserId",
                table: "Gallery");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Gallery");
        }
    }
}
