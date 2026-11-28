-- Review SQL for 20261128113200_ExpandEmployeeProfile; manually authored, not executed.
-- Apply on CRM only after reviewing the EF migration and backing up the database.
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
        THROW 50001, 'CRM migration history not found.', 1;
    IF EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261128113200_ExpandEmployeeProfile')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;
    IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261128090000_AddHumanResources')
        THROW 50002, 'Apply the initial HR migration first.', 1;
    IF OBJECT_ID(N'dbo.HrEmployeeProfiles', N'U') IS NULL
        THROW 50003, 'HR employee table not found.', 1;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.HrEmployeeProfiles') AND name IN (N'Address', N'BirthCertificateNumber', N'BirthDate', N'Education', N'Email', N'EmergencyContactName', N'EmergencyContactPhone', N'FatherName', N'Mobile', N'NationalCode', N'Telephone'))
        THROW 50004, 'One or more profile columns already exist without migration history; review the schema.', 1;
    ALTER TABLE [dbo].[HrEmployeeProfiles] ADD
        [Address] nvarchar(500) NULL,
        [BirthCertificateNumber] nvarchar(32) NULL,
        [BirthDate] date NULL,
        [Education] nvarchar(100) NULL,
        [Email] nvarchar(254) NULL,
        [EmergencyContactName] nvarchar(150) NULL,
        [EmergencyContactPhone] nvarchar(24) NULL,
        [FatherName] nvarchar(100) NULL,
        [Mobile] nvarchar(24) NULL,
        [NationalCode] nvarchar(10) NULL,
        [Telephone] nvarchar(24) NULL;
    CREATE UNIQUE INDEX [IX_HrEmployeeProfiles_NationalCode] ON [dbo].[HrEmployeeProfiles] ([NationalCode]) WHERE [NationalCode] IS NOT NULL;
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES (N'20261128113200_ExpandEmployeeProfile', N'10.0.14');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
