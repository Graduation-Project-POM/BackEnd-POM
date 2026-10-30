using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIO.Migrations
{
    /// <summary>
    /// Brings Gallery in line with the model (Category + Size, no Url). The
    /// snapshot already had this shape but no migration ever created it, so
    /// databases built from migrations were missing it. Guarded so databases
    /// that already match (e.g. ones updated by hand) are left untouched.
    /// </summary>
    public partial class SyncGalleryColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Gallery', N'Category') IS NULL
    ALTER TABLE [Gallery] ADD [Category] int NOT NULL CONSTRAINT [DF_Gallery_Category] DEFAULT 0;

IF COL_LENGTH(N'dbo.Gallery', N'Size') IS NULL
    ALTER TABLE [Gallery] ADD [Size] bigint NULL;

IF COL_LENGTH(N'dbo.Gallery', N'Url') IS NOT NULL
BEGIN
    DECLARE @df sysname;
    SELECT @df = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE [d].[parent_object_id] = OBJECT_ID(N'[Gallery]') AND [c].[name] = N'Url';
    IF @df IS NOT NULL EXEC(N'ALTER TABLE [Gallery] DROP CONSTRAINT [' + @df + N'];');
    ALTER TABLE [Gallery] DROP COLUMN [Url];
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Gallery', N'Url') IS NULL
    ALTER TABLE [Gallery] ADD [Url] nvarchar(max) NOT NULL CONSTRAINT [DF_Gallery_Url] DEFAULT N'';

IF COL_LENGTH(N'dbo.Gallery', N'Size') IS NOT NULL
    ALTER TABLE [Gallery] DROP COLUMN [Size];

IF COL_LENGTH(N'dbo.Gallery', N'Category') IS NOT NULL
BEGIN
    DECLARE @df sysname;
    SELECT @df = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE [d].[parent_object_id] = OBJECT_ID(N'[Gallery]') AND [c].[name] = N'Category';
    IF @df IS NOT NULL EXEC(N'ALTER TABLE [Gallery] DROP CONSTRAINT [' + @df + N'];');
    ALTER TABLE [Gallery] DROP COLUMN [Category];
END
");
        }
    }
}
