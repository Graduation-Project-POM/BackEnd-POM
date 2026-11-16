using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MIO.Infrastructure;

#nullable disable

namespace MIO.Migrations
{
    /// <summary>
    /// Data-only migration (no schema change, nothing deleted): recomputes the
    /// stored "solved" counters with the new rule — solved = submitted a
    /// solution (problem bank or contest), instead of graded with a passing
    /// score. Mirrors Infrastructure/Services/SolvedCounters.cs.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261117120000_RecomputeSolvedCountsFromSubmissions")]
    public partial class RecomputeSolvedCountsFromSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Problems: distinct students who submitted.
            migrationBuilder.Sql(@"
UPDATE p SET SolvedCount = (
    SELECT COUNT(*) FROM (
        SELECT b.StudentId FROM ProblemBankSubmissions b WHERE b.ProblemId = p.ID
        UNION
        SELECT ps.StudentId FROM ProblemStudents ps WHERE ps.ProblemId = p.ID
    ) x)
FROM Problems p;");

            // Students: distinct problems submitted.
            migrationBuilder.Sql(@"
UPDATE s SET ProblemSolved = (
    SELECT COUNT(*) FROM (
        SELECT b.ProblemId FROM ProblemBankSubmissions b WHERE b.StudentId = s.Id
        UNION
        SELECT ps.ProblemId FROM ProblemStudents ps WHERE ps.StudentId = s.Id
    ) x)
FROM Students s;");

            // Contest participants: the contest's problems submitted.
            migrationBuilder.Sql(@"
UPDATE cs SET ProblemSolved = (
    SELECT COUNT(DISTINCT ps.ProblemId)
    FROM ProblemStudents ps
    JOIN ContestProblem cp ON cp.ProblemsID = ps.ProblemId AND cp.ContestsId = cs.ContestId
    WHERE ps.StudentId = cs.StudentId)
FROM ContestStudents cs;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the previous rule: graded with Score >= half the problem's points.
            migrationBuilder.Sql(@"
UPDATE p SET SolvedCount = (
    SELECT COUNT(*) FROM ProblemStudents ps
    WHERE ps.ProblemId = p.ID AND ps.Score IS NOT NULL AND ps.Score * 2 >= p.Points)
FROM Problems p;");

            migrationBuilder.Sql(@"
UPDATE s SET ProblemSolved = (
    SELECT COUNT(*) FROM ProblemStudents ps JOIN Problems p ON p.ID = ps.ProblemId
    WHERE ps.StudentId = s.Id AND ps.Score IS NOT NULL AND ps.Score * 2 >= p.Points)
FROM Students s;");

            migrationBuilder.Sql(@"
UPDATE cs SET ProblemSolved = (
    SELECT COUNT(*) FROM ProblemStudents ps
    JOIN Problems p ON p.ID = ps.ProblemId
    JOIN ContestProblem cp ON cp.ProblemsID = ps.ProblemId AND cp.ContestsId = cs.ContestId
    WHERE ps.StudentId = cs.StudentId AND ps.Score IS NOT NULL AND ps.Score * 2 >= p.Points)
FROM ContestStudents cs;");
        }
    }
}
