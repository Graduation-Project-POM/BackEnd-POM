using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIO.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: GradedAt already exists (added by AddSubmissionGrade), so only
            // Score and Feedback are added here.
            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "ProblemStudents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "ProblemStudents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "ProblemStudents");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "ProblemStudents");
        }
    }
}
