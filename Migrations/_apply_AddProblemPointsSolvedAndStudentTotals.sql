SET XACT_ABORT ON;
BEGIN TRAN;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'TotalPoints')
    ALTER TABLE dbo.Students ADD TotalPoints int NOT NULL CONSTRAINT DF_Students_TotalPoints DEFAULT (0);

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Problems') AND name = 'IsBankVisible')
    ALTER TABLE dbo.Problems ADD IsBankVisible bit NOT NULL CONSTRAINT DF_Problems_IsBankVisible DEFAULT (1);

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Problems') AND name = 'Points')
    ALTER TABLE dbo.Problems ADD Points int NOT NULL CONSTRAINT DF_Problems_Points DEFAULT (100);

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Problems') AND name = 'SolvedCount')
    ALTER TABLE dbo.Problems ADD SolvedCount int NOT NULL CONSTRAINT DF_Problems_SolvedCount DEFAULT (0);

COMMIT;
GO

UPDATE p SET p.SolvedCount =
    (SELECT COUNT(*) FROM dbo.ProblemStudents ps
     WHERE ps.ProblemId = p.ID AND ps.Score = p.Points)
FROM dbo.Problems p;

UPDATE s SET s.TotalPoints =
    ISNULL((SELECT SUM(ps.Score) FROM dbo.ProblemStudents ps
            WHERE ps.StudentId = s.Id AND ps.Score IS NOT NULL), 0)
FROM dbo.Students s;

UPDATE s SET s.ProblemSolved =
    (SELECT COUNT(*) FROM dbo.ProblemStudents ps
     INNER JOIN dbo.Problems p ON p.ID = ps.ProblemId
     WHERE ps.StudentId = s.Id AND ps.Score = p.Points)
FROM dbo.Students s;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20260622140557_AddProblemPointsSolvedAndStudentTotals')
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260622140557_AddProblemPointsSolvedAndStudentTotals', '9.0.0');
GO
