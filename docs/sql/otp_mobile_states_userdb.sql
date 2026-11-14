-- Run once against the UserConnection database (UserDb), NOT CRMDb.
-- Review and back up before applying. No production database is changed by this file.
IF OBJECT_ID(N'dbo.OtpMobileStates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OtpMobileStates
    (
        Mobile nvarchar(11) NOT NULL CONSTRAINT PK_OtpMobileStates PRIMARY KEY,
        OtpType nvarchar(10) NULL,
        CodeSalt varbinary(max) NULL,
        CodeHash varbinary(max) NULL,
        OtpExpiresAtUtc datetime2(7) NULL,
        FailedAttempts int NOT NULL CONSTRAINT DF_OtpMobileStates_FailedAttempts DEFAULT (0),
        LockoutCount int NOT NULL CONSTRAINT DF_OtpMobileStates_LockoutCount DEFAULT (0),
        LockedUntilUtc datetime2(7) NULL
    );
END;
