using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIO.Migrations
{
    /// <inheritdoc />
    public partial class addedContestStudentAndSomeChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contests_Trainers_TrainerId",
                table: "Contests");

            migrationBuilder.DropForeignKey(
                name: "FK_Problems_Contests_ContestId",
                table: "Problems");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Contests_ContestId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_ContestId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Problems_ContestId",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "ContestId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ContestId",
                table: "Problems");

            migrationBuilder.CreateTable(
                name: "ContestProblem",
                columns: table => new
                {
                    ContestsId = table.Column<int>(type: "int", nullable: false),
                    ProblemsID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContestProblem", x => new { x.ContestsId, x.ProblemsID });
                    table.ForeignKey(
                        name: "FK_ContestProblem_Contests_ContestsId",
                        column: x => x.ContestsId,
                        principalTable: "Contests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContestProblem_Problems_ProblemsID",
                        column: x => x.ProblemsID,
                        principalTable: "Problems",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContestStudents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ContestId = table.Column<int>(type: "int", nullable: false),
                    ProblemSolved = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    TimeToFinishTheContest = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContestStudents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContestStudents_Contests_ContestId",
                        column: x => x.ContestId,
                        principalTable: "Contests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContestStudents_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContestProblem_ProblemsID",
                table: "ContestProblem",
                column: "ProblemsID");

            migrationBuilder.CreateIndex(
                name: "IX_ContestStudents_ContestId",
                table: "ContestStudents",
                column: "ContestId");

            migrationBuilder.CreateIndex(
                name: "IX_ContestStudents_StudentId",
                table: "ContestStudents",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contests_Trainers_TrainerId",
                table: "Contests",
                column: "TrainerId",
                principalTable: "Trainers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contests_Trainers_TrainerId",
                table: "Contests");

            migrationBuilder.DropTable(
                name: "ContestProblem");

            migrationBuilder.DropTable(
                name: "ContestStudents");

            migrationBuilder.AddColumn<int>(
                name: "ContestId",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContestId",
                table: "Problems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_ContestId",
                table: "Students",
                column: "ContestId");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_ContestId",
                table: "Problems",
                column: "ContestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contests_Trainers_TrainerId",
                table: "Contests",
                column: "TrainerId",
                principalTable: "Trainers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Problems_Contests_ContestId",
                table: "Problems",
                column: "ContestId",
                principalTable: "Contests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Contests_ContestId",
                table: "Students",
                column: "ContestId",
                principalTable: "Contests",
                principalColumn: "Id");
        }
    }
}
