using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIO.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionIsPublished : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "ProblemBankSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "ProblemStudents",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "ProblemStudents");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "ProblemBankSubmissions");
        }
    }
}
