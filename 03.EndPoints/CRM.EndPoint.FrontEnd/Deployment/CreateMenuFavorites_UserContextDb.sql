/*
  ERP menu / UserContextDb only
  Migration: 20261122130000_AddMenuFavorites
  Run in SSMS against the database configured by ConnectionStrings:UserConnection.
  Do not execute in the CRMConnection database.
  Do not run an EF database update for the same migration afterwards;
  this script records that specific migration in __EFMigrationsHistory.
  All existing user/role and authorization tables are untouched.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @MigrationId nvarchar(150) = N'20261122130000_AddMenuFavorites';
DECLARE @ExpectedDb sysname = DB_NAME();
PRINT N'Target database: ' + @ExpectedDb;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    THROW 51000, 'EF migration history not found. Stop and verify UserConnection target database.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.MenuFavorites', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[MenuFavorites]
        (
            [UserId] nvarchar(450) NOT NULL,
            [MenuId] nvarchar(100) NOT NULL,
            [SortOrder] int NOT NULL,
            CONSTRAINT [PK_MenuFavorites] PRIMARY KEY ([UserId], [MenuId])
        );
    END;

    -- Refuse to record a migration as applied if the existing table has a
    -- different model from the checked-in EF Core migration.
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns AS c
        JOIN sys.types AS t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(N'dbo.MenuFavorites')
          AND c.name = N'UserId'
          AND t.name = N'nvarchar'
          AND c.max_length = 900
          AND c.is_nullable = 0
    ) OR NOT EXISTS (
        SELECT 1 FROM sys.columns AS c
        JOIN sys.types AS t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(N'dbo.MenuFavorites')
          AND c.name = N'MenuId'
          AND t.name = N'nvarchar'
          AND c.max_length = 200
          AND c.is_nullable = 0
    ) OR NOT EXISTS (
        SELECT 1 FROM sys.columns AS c
        JOIN sys.types AS t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(N'dbo.MenuFavorites')
          AND c.name = N'SortOrder'
          AND t.name = N'int'
          AND c.is_nullable = 0
    ) OR NOT EXISTS (
        SELECT 1 FROM sys.key_constraints AS k
        WHERE k.parent_object_id = OBJECT_ID(N'dbo.MenuFavorites')
          AND k.type = 'PK'
          AND k.name = N'PK_MenuFavorites'
    )
    BEGIN
        THROW 51001, 'Existing MenuFavorites does not match the migration schema. No migration history changed.', 1;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = @MigrationId
    )
    BEGIN
        INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (@MigrationId, N'10.0.14');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT DB_NAME() AS [DatabaseName],
       OBJECT_ID(N'dbo.MenuFavorites', N'U') AS [MenuFavoritesObjectId],
       (SELECT COUNT(*) FROM [dbo].[__EFMigrationsHistory]
        WHERE [MigrationId] = @MigrationId) AS [MigrationHistoryEntryCount];
