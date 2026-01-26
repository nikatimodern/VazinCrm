BEGIN TRANSACTION;
DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PreFactors]') AND [c].[name] = N'DealSerialNumber');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [PreFactors] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [PreFactors] DROP COLUMN [DealSerialNumber];

ALTER TABLE [Reports] ADD [DealId] uniqueidentifier NULL;

ALTER TABLE [Reports] ADD [DealSerialNumber] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250908103635_up14040617', N'9.0.5');

COMMIT;
GO

