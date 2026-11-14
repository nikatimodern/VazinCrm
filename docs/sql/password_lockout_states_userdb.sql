-- Alternative to generating an EF Core migration for UserContextDb.
-- Run against the UserConnection database only. Do not run after applying a migration
-- that creates the PasswordLockoutStates table.
-- Make a database backup and review this script before executing.
IF OBJECT_ID(N'dbo.PasswordLockoutStates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordLockoutStates
    (
        UserId nvarchar(450) NOT NULL
            CONSTRAINT PK_PasswordLockoutStates PRIMARY KEY,
        FailedAttempts int NOT NULL
            CONSTRAINT DF_PasswordLockoutStates_FailedAttempts DEFAULT (0),
        LockoutCount int NOT NULL
            CONSTRAINT DF_PasswordLockoutStates_LockoutCount DEFAULT (0),
        LockedUntilUtc datetime2(7) NULL
    );
END;
