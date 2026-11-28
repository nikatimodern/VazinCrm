-- Review SQL, CRM only. Hand-authored; not executed. Back up and verify EF-generated SQL first.
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
        THROW 50001, 'CRM migration history not found.', 1;
    IF EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261128120000_AddEmployeeLocationAndEducationDocument')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;
    IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261128113200_ExpandEmployeeProfile')
        THROW 50002, 'Apply ExpandEmployeeProfile first.', 1;
    IF OBJECT_ID(N'dbo.HrEmployeeProfiles', N'U') IS NULL
        THROW 50003, 'Employee profile table not found.', 1;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.HrEmployeeProfiles') AND name IN (N'Latitude', N'Longitude', N'EducationDocumentContent', N'EducationDocumentType'))
        THROW 50004, 'Columns already exist without migration history. Review schema.', 1;
    ALTER TABLE dbo.HrEmployeeProfiles ADD Latitude decimal(9,6) NULL, Longitude decimal(9,6) NULL,
        EducationDocumentContent varbinary(max) NULL, EducationDocumentType nvarchar(32) NULL;
    ALTER TABLE dbo.HrEmployeeProfiles ADD CONSTRAINT CK_HrEmployeeProfiles_Coordinates CHECK (([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL AND [Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180));
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES (N'20261128120000_AddEmployeeLocationAndEducationDocument', N'10.0.14');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
