-- Run against the CRM database before deploying this branch.
-- Existing records retain NULL because their actual creation date is unknown.
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.BaseDescriptions', N'U') IS NULL
    THROW 50001, 'BaseDescriptions table was not found in the current database.', 1;
GO
IF COL_LENGTH(N'dbo.BaseDescriptions', N'CreateDate') IS NULL
BEGIN
    ALTER TABLE dbo.BaseDescriptions ADD CreateDate datetime2(7) NULL;
END;
GO
