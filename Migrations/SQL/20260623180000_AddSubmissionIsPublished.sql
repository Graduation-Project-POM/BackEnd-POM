-- Adds IsPublished (bit NOT NULL, default 1) to both submission tables so a
-- student can flip their own row to a draft and hide it from the community
-- list without losing the submission. Idempotent (uses IF NOT EXISTS).

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'IsPublished' AND Object_ID = Object_ID(N'dbo.ProblemBankSubmissions')
)
BEGIN
    ALTER TABLE dbo.ProblemBankSubmissions
        ADD IsPublished bit NOT NULL CONSTRAINT DF_ProblemBankSubmissions_IsPublished DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'IsPublished' AND Object_ID = Object_ID(N'dbo.ProblemStudents')
)
BEGIN
    ALTER TABLE dbo.ProblemStudents
        ADD IsPublished bit NOT NULL CONSTRAINT DF_ProblemStudents_IsPublished DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20260623180000_AddSubmissionIsPublished'
)
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260623180000_AddSubmissionIsPublished', N'9.0.0');
END;
