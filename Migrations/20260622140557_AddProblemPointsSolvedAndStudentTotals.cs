using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIO.Migrations
{
    /// <inheritdoc />
    public partial class AddProblemPointsSolvedAndStudentTotals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalPoints",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsBankVisible",
                table: "Problems",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Problems",
                type: "int",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "SolvedCount",
                table: "Problems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE p SET p.SolvedCount =
                    (SELECT COUNT(*) FROM ProblemStudents ps
                     WHERE ps.ProblemId = p.ID AND ps.Score = p.Points)
                FROM Problems p;");

            migrationBuilder.Sql(@"
                UPDATE s SET s.TotalPoints =
                    ISNULL((SELECT SUM(ps.Score) FROM ProblemStudents ps
                            WHERE ps.StudentId = s.Id AND ps.Score IS NOT NULL), 0)
                FROM Students s;");

            migrationBuilder.Sql(@"
                UPDATE s SET s.ProblemSolved =
                    (SELECT COUNT(*) FROM ProblemStudents ps
                     INNER JOIN Problems p ON p.ID = ps.ProblemId
                     WHERE ps.StudentId = s.Id AND ps.Score = p.Points)
                FROM Students s;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalPoints",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "IsBankVisible",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "SolvedCount",
                table: "Problems");
        }
    }
}
