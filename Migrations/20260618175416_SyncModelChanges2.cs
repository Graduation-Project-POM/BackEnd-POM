using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIO.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Grade",
                table: "ProblemStudents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Grade",
                table: "ProblemStudents",
                type: "int",
                nullable: true);
        }
    }
}
