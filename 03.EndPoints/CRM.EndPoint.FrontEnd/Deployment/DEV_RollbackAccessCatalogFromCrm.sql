-- DEVELOPMENT / TEST ONLY.
-- Connect to the database configured by CRMConnection (NOT UserConnection).
-- Deletes ONLY the three disposable trial Access* tables and unregisters ONLY the two trial migrations.
-- Do NOT run on a database that stores live access-control data.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AccessRolePermissions', N'U') IS NOT NULL
        DROP TABLE dbo.AccessRolePermissions;
    IF OBJECT_ID(N'dbo.AccessPermissions', N'U') IS NOT NULL
        DROP TABLE dbo.AccessPermissions;
    IF OBJECT_ID(N'dbo.AccessEntities', N'U') IS NOT NULL
        DROP TABLE dbo.AccessEntities;

    IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
        DELETE FROM dbo.__EFMigrationsHistory
        WHERE MigrationId IN (
            N'20261110180000_AddAccessCatalog',
            N'20261110200000_AddAccessRolePermissions'
        );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
