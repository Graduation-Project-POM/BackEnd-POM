using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIO.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProblemStudents_ProblemId",
                table: "ProblemStudents");

            migrationBuilder.DropIndex(
                name: "IX_GroupStudents_GroupId",
                table: "GroupStudents");

            migrationBuilder.DropIndex(
                name: "IX_GroupContests_GroupId",
                table: "GroupContests");

            migrationBuilder.DropIndex(
                name: "IX_ContestStudents_ContestId",
                table: "ContestStudents");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProblemStudents_ProblemId_StudentId",
                table: "ProblemStudents",
                columns: new[] { "ProblemId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupStudents_GroupId_StudentId",
                table: "GroupStudents",
                columns: new[] { "GroupId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupContests_GroupId_ContestId",
                table: "GroupContests",
                columns: new[] { "GroupId", "ContestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContestStudents_ContestId_StudentId",
                table: "ContestStudents",
                columns: new[] { "ContestId", "StudentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_UserName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ProblemStudents_ProblemId_StudentId",
                table: "ProblemStudents");

            migrationBuilder.DropIndex(
                name: "IX_GroupStudents_GroupId_StudentId",
                table: "GroupStudents");

            migrationBuilder.DropIndex(
                name: "IX_GroupContests_GroupId_ContestId",
                table: "GroupContests");

            migrationBuilder.DropIndex(
                name: "IX_ContestStudents_ContestId_StudentId",
                table: "ContestStudents");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.CreateIndex(
                name: "IX_ProblemStudents_ProblemId",
                table: "ProblemStudents",
                column: "ProblemId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupStudents_GroupId",
                table: "GroupStudents",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupContests_GroupId",
                table: "GroupContests",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ContestStudents_ContestId",
                table: "ContestStudents",
                column: "ContestId");
        }
    }
}
