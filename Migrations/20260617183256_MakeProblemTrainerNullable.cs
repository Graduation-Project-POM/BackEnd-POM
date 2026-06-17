using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIO.Migrations
{
    /// <inheritdoc />
    public partial class MakeProblemTrainerNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only the Problem.TrainerID nullability change. (EF scaffolded
            // unrelated model drift — Gallery/Groups/etc. — which is left to a
            // separate migration to avoid failing on pre-existing schema gaps.)
            migrationBuilder.AlterColumn<int>(
                name: "TrainerID",
                table: "Problems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TrainerID",
                table: "Problems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
