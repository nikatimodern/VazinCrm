-- Hand-authored review script matching 20261128090000_AddHumanResources. Not EF-generated or executed.
-- Run on the CRM database only, after validating the actual EF migration list and backup.
-- Assumes dbo is the default CRM schema; does not modify Identity, Sepidar, or existing business tables.
-- This script and EF Update-Database are alternatives; do not apply both independently.
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
        THROW 50001, 'CRM migration history not found. Check target database.', 1;
    IF EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261128090000_AddHumanResources')
    BEGIN
        COMMIT TRANSACTION;
        PRINT 'HR migration already recorded.';
        RETURN;
    END;
    IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261125110000_AddPrefactorTermsCreateDate')
        THROW 50002, 'Expected CRM baseline migration not found. Apply earlier CRM migrations first.', 1;
    IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo') AND name IN (N'HrEmployeeProfiles', N'HrDepartments', N'HrJobPositions', N'HrEmploymentRecords', N'HrEmployeeAssignments', N'HrEmploymentContracts'))
        THROW 50003, 'HR table already exists without migration history. Inspect schema; do not overwrite.', 1;

    CREATE TABLE [dbo].[HrEmployeeProfiles] (
        [Id] uniqueidentifier NOT NULL,
        [AppUserId] nvarchar(450) NULL,
        [CommentText] nvarchar(max) NULL,
        [CreateDate] datetime2 NOT NULL,
        [CreateDateTxt] nvarchar(max) NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [ModifyDate] datetime2 NULL,
        [ModifyDateTxt] nvarchar(max) NULL,
        [PersonnelCode] nvarchar(40) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [UserCreator] nvarchar(max) NULL,
        [UserCreatorID] nvarchar(max) NULL,
        [UserModify] nvarchar(max) NULL,
        [UserModifyID] nvarchar(max) NULL,
        CONSTRAINT [PK_HrEmployeeProfiles] PRIMARY KEY ([Id])
    );

    CREATE TABLE [dbo].[HrDepartments] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(40) NOT NULL,
        [CommentText] nvarchar(max) NULL,
        [CreateDate] datetime2 NOT NULL,
        [CreateDateTxt] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [IsEnabled] bit NOT NULL,
        [ModifyDate] datetime2 NULL,
        [ModifyDateTxt] nvarchar(max) NULL,
        [Name] nvarchar(150) NOT NULL,
        [ParentId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [UserCreator] nvarchar(max) NULL,
        [UserCreatorID] nvarchar(max) NULL,
        [UserModify] nvarchar(max) NULL,
        [UserModifyID] nvarchar(max) NULL,
        CONSTRAINT [PK_HrDepartments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_HrDepartments_Parent] CHECK ([ParentId] IS NULL OR [ParentId] <> [Id]),
        CONSTRAINT [FK_HrDepartments_HrDepartments_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [dbo].[HrDepartments] ([Id]) ON DELETE NO ACTION
    );

    CREATE TABLE [dbo].[HrJobPositions] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(40) NOT NULL,
        [CommentText] nvarchar(max) NULL,
        [CreateDate] datetime2 NOT NULL,
        [CreateDateTxt] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [IsEnabled] bit NOT NULL,
        [LegacyPositionId] uniqueidentifier NULL,
        [ModifyDate] datetime2 NULL,
        [ModifyDateTxt] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [UserCreator] nvarchar(max) NULL,
        [UserCreatorID] nvarchar(max) NULL,
        [UserModify] nvarchar(max) NULL,
        [UserModifyID] nvarchar(max) NULL,
        CONSTRAINT [PK_HrJobPositions] PRIMARY KEY ([Id])
    );

    CREATE TABLE [dbo].[HrEmploymentRecords] (
        [Id] uniqueidentifier NOT NULL,
        [CommentText] nvarchar(max) NULL,
        [CreateDate] datetime2 NOT NULL,
        [CreateDateTxt] nvarchar(max) NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [EmploymentType] nvarchar(64) NOT NULL,
        [EndDate] date NULL,
        [IsDeleted] bit NOT NULL,
        [ModifyDate] datetime2 NULL,
        [ModifyDateTxt] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        [StartDate] date NOT NULL,
        [UserCreator] nvarchar(max) NULL,
        [UserCreatorID] nvarchar(max) NULL,
        [UserModify] nvarchar(max) NULL,
        [UserModifyID] nvarchar(max) NULL,
        CONSTRAINT [PK_HrEmploymentRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_HrEmploymentRecords_Dates] CHECK ([EndDate] IS NULL OR [EndDate] >= [StartDate]),
        CONSTRAINT [FK_HrEmploymentRecords_HrEmployeeProfiles_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[HrEmployeeProfiles] ([Id]) ON DELETE NO ACTION
    );

    CREATE TABLE [dbo].[HrEmployeeAssignments] (
        [Id] uniqueidentifier NOT NULL,
        [CommentText] nvarchar(max) NULL,
        [CreateDate] datetime2 NOT NULL,
        [CreateDateTxt] nvarchar(max) NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [IsDeleted] bit NOT NULL,
        [IsPrimary] bit NOT NULL,
        [ManagerEmployeeId] uniqueidentifier NULL,
        [ModifyDate] datetime2 NULL,
        [ModifyDateTxt] nvarchar(max) NULL,
        [PositionId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [UserCreator] nvarchar(max) NULL,
        [UserCreatorID] nvarchar(max) NULL,
        [UserModify] nvarchar(max) NULL,
        [UserModifyID] nvarchar(max) NULL,
        CONSTRAINT [PK_HrEmployeeAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_HrEmployeeAssignments_Dates] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT [CK_HrEmployeeAssignments_Manager] CHECK ([ManagerEmployeeId] IS NULL OR [ManagerEmployeeId] <> [EmployeeId]),
        CONSTRAINT [FK_HrEmployeeAssignments_HrDepartments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[HrDepartments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HrEmployeeAssignments_HrEmployeeProfiles_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[HrEmployeeProfiles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HrEmployeeAssignments_HrEmployeeProfiles_ManagerEmployeeId] FOREIGN KEY ([ManagerEmployeeId]) REFERENCES [dbo].[HrEmployeeProfiles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HrEmployeeAssignments_HrJobPositions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [dbo].[HrJobPositions] ([Id]) ON DELETE NO ACTION
    );

    CREATE TABLE [dbo].[HrEmploymentContracts] (
        [Id] uniqueidentifier NOT NULL,
        [AgreementDate] date NOT NULL,
        [ApprovedAtUtc] datetimeoffset NULL,
        [ApprovedByUserId] nvarchar(450) NULL,
        [CommentText] nvarchar(max) NULL,
        [ContractNumber] nvarchar(64) NOT NULL,
        [CreateDate] datetime2 NOT NULL,
        [CreateDateTxt] nvarchar(max) NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [EndDate] date NULL,
        [IsDeleted] bit NOT NULL,
        [ModifyDate] datetime2 NULL,
        [ModifyDateTxt] nvarchar(max) NULL,
        [PreviousContractId] uniqueidentifier NULL,
        [ResolutionReason] nvarchar(1000) NULL,
        [ResolvedAtUtc] datetimeoffset NULL,
        [ResolvedByUserId] nvarchar(450) NULL,
        [RowVersion] rowversion NOT NULL,
        [StartDate] date NOT NULL,
        [Status] int NOT NULL,
        [Term] int NOT NULL,
        [TerminatedOn] date NULL,
        [UserCreator] nvarchar(max) NULL,
        [UserCreatorID] nvarchar(max) NULL,
        [UserModify] nvarchar(max) NULL,
        [UserModifyID] nvarchar(max) NULL,
        CONSTRAINT [PK_HrEmploymentContracts] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_HrEmploymentContracts_EmployeeId_Id] UNIQUE ([EmployeeId], [Id]),
        CONSTRAINT [CK_HrContracts_Term] CHECK (([Term] = 1 AND [EndDate] IS NOT NULL AND [EndDate] >= [StartDate]) OR ([Term] = 2 AND [EndDate] IS NULL)),
        CONSTRAINT [CK_HrContracts_Predecessor] CHECK ([PreviousContractId] IS NULL OR [PreviousContractId] <> [Id]),
        CONSTRAINT [CK_HrContracts_Status] CHECK ([Status] IN (1, 2, 3, 4)),
        CONSTRAINT [CK_HrContracts_Termination] CHECK (([Status] = 4 AND [TerminatedOn] IS NOT NULL AND [TerminatedOn] >= [StartDate] AND ([EndDate] IS NULL OR [TerminatedOn] <= [EndDate])) OR ([Status] <> 4 AND [TerminatedOn] IS NULL)),
        CONSTRAINT [CK_HrContracts_Approval] CHECK (([Status] IN (2, 4) AND [ApprovedByUserId] IS NOT NULL AND [ApprovedAtUtc] IS NOT NULL) OR ([Status] IN (1, 3) AND [ApprovedByUserId] IS NULL AND [ApprovedAtUtc] IS NULL)),
        CONSTRAINT [CK_HrContracts_Resolution] CHECK (([Status] IN (3, 4) AND [ResolvedByUserId] IS NOT NULL AND [ResolvedAtUtc] IS NOT NULL AND [ResolutionReason] IS NOT NULL) OR ([Status] IN (1, 2) AND [ResolvedByUserId] IS NULL AND [ResolvedAtUtc] IS NULL AND [ResolutionReason] IS NULL)),
        CONSTRAINT [FK_HrEmploymentContracts_HrEmployeeProfiles_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[HrEmployeeProfiles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HrEmploymentContracts_HrEmploymentContracts_EmployeeId_PreviousContractId] FOREIGN KEY ([EmployeeId], [PreviousContractId]) REFERENCES [dbo].[HrEmploymentContracts] ([EmployeeId], [Id]) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX [IX_HrEmployeeProfiles_AppUserId] ON [dbo].[HrEmployeeProfiles] ([AppUserId]) WHERE [AppUserId] IS NOT NULL;
    CREATE UNIQUE INDEX [IX_HrEmployeeProfiles_PersonnelCode] ON [dbo].[HrEmployeeProfiles] ([PersonnelCode]);
    CREATE UNIQUE INDEX [IX_HrDepartments_Code] ON [dbo].[HrDepartments] ([Code]);
    CREATE INDEX [IX_HrDepartments_ParentId] ON [dbo].[HrDepartments] ([ParentId]);
    CREATE UNIQUE INDEX [IX_HrJobPositions_Code] ON [dbo].[HrJobPositions] ([Code]);
    CREATE UNIQUE INDEX [IX_HrEmploymentRecords_EmployeeId_StartDate] ON [dbo].[HrEmploymentRecords] ([EmployeeId], [StartDate]);
    CREATE INDEX [IX_HrEmployeeAssignments_DepartmentId] ON [dbo].[HrEmployeeAssignments] ([DepartmentId]);
    CREATE INDEX [IX_HrEmployeeAssignments_EmployeeId_EffectiveFrom] ON [dbo].[HrEmployeeAssignments] ([EmployeeId], [EffectiveFrom]);
    CREATE INDEX [IX_HrEmployeeAssignments_ManagerEmployeeId] ON [dbo].[HrEmployeeAssignments] ([ManagerEmployeeId]);
    CREATE INDEX [IX_HrEmployeeAssignments_PositionId] ON [dbo].[HrEmployeeAssignments] ([PositionId]);
    CREATE UNIQUE INDEX [IX_HrEmploymentContracts_ContractNumber] ON [dbo].[HrEmploymentContracts] ([ContractNumber]);
    CREATE INDEX [IX_HrEmploymentContracts_EmployeeId_PreviousContractId] ON [dbo].[HrEmploymentContracts] ([EmployeeId], [PreviousContractId]);
    CREATE INDEX [IX_HrEmploymentContracts_EmployeeId_StartDate] ON [dbo].[HrEmploymentContracts] ([EmployeeId], [StartDate]);
    CREATE INDEX [IX_HrEmploymentContracts_Status_EndDate_Id] ON [dbo].[HrEmploymentContracts] ([Status], [EndDate], [Id]);

    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20261128090000_AddHumanResources', N'10.0.14');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
