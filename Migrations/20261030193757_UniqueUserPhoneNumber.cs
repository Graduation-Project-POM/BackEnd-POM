using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIO.Migrations
{
    /// <inheritdoc />
    public partial class UniqueUserPhoneNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bring existing rows in line with PhoneNumbers.Normalize so the
            // index compares numbers the same way the API does: drop the usual
            // separators, and store blanks as NULL.
            migrationBuilder.Sql(@"
UPDATE [Users]
SET [PhoneNumber] = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM([PhoneNumber])),
    ' ', ''), '-', ''), '(', ''), ')', ''), '.', ''), CHAR(9), '')
WHERE [PhoneNumber] IS NOT NULL;

UPDATE [Users] SET [PhoneNumber] = NULL WHERE [PhoneNumber] = '';");

            // A number shared by several accounts stays with the account that
            // registered it first; the later ones have it cleared (no account
            // is deleted) so the unique index can be created.
            migrationBuilder.Sql(@"
WITH Ranked AS (
    SELECT [PhoneNumber],
           ROW_NUMBER() OVER (PARTITION BY [PhoneNumber] ORDER BY [CreatedAt], [Id]) AS rn
    FROM [Users]
    WHERE [PhoneNumber] IS NOT NULL
)
UPDATE Ranked SET [PhoneNumber] = NULL WHERE rn > 1;");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users");
        }
    }
}
