-- Adds Constraints (free text) and Examples (JSON list of {input,output,explanation})
-- columns to the Problems table, then records the migration so EF stops trying to
-- re-apply it. EF's `database update` is broken in this repo — apply this script
-- directly against the dev database (sqlcmd / SSMS / Rider DB tool).

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'Constraints' AND Object_ID = Object_ID(N'dbo.Problems')
)
BEGIN
    ALTER TABLE dbo.Problems ADD Constraints nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'Examples' AND Object_ID = Object_ID(N'dbo.Problems')
)
BEGIN
    ALTER TABLE dbo.Problems ADD Examples nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20260623120000_AddProblemConstraintsAndExamples'
)
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260623120000_AddProblemConstraintsAndExamples', N'9.0.0');
END;
