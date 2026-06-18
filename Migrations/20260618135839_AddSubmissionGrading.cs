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
            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "ProblemStudents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "ProblemStudents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "ProblemStudents",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "ProblemStudents");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "ProblemStudents");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "ProblemStudents");
        }
    }
}
