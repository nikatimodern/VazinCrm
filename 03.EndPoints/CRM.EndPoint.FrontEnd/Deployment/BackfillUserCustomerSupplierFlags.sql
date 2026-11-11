/*
    CRM access control / Identity user-kind backfill
    ------------------------------------------------
    SAFE DEFAULT: @Apply = 0 (PREVIEW ONLY).
    Changes only dbo.AspNetUsers.IsCustomer and IsSupplier in the CURRENT database.
    Never changes IsEmployee or any data in CRM.dbo.Customers.

    BEFORE RUNNING:
    1. Connect SSMS to the SQL instance containing BOTH UserConnection and CRMConnection databases.
    2. Select the USER database (UserConnection) in the SSMS database dropdown.
    3. Replace @CrmDatabase with the actual database name behind CRMConnection.
    4. Run with @Apply=0; review the summary, proposed updates, and unmatched/ambiguous records.
    5. Back up the User database, set @Apply=1 and run the whole script again.

    If the two connections use DIFFERENT SQL SERVER INSTANCES, do not run this script:
    use a two-DbContext application backfill or stage data securely first.

    Match precedence:
      a. AspNetUsers.PersonId = Customers.Id (authoritative);
      b. only when PersonId IS NULL, an EXACTLY UNIQUE normalized phone number.
    Duplicate phone matches, invalid RoleType, deleted customers and orphan PersonIds
    are not automatically classified.

    Customer.RoleType: 1 = customer, 2 = supplier, 3 = both.
    Additive update: existing true flags are NEVER turned off; IsEmployee is untouched.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @CrmDatabase sysname = N'__SET_CRM_DATABASE_NAME__';
DECLARE @Apply bit = 0;
DECLARE @UseUniqueMobile bit = 1;
DECLARE @UserDatabase sysname = DB_NAME();

IF @UserDatabase IS NULL OR @UserDatabase IN (N'master', N'model', N'msdb', N'tempdb')
    THROW 51001, N'Select the UserConnection database in SSMS before running.', 1;
IF @CrmDatabase = N'__SET_CRM_DATABASE_NAME__' OR @CrmDatabase IS NULL
    THROW 51002, N'Fill in @CrmDatabase with the CRMConnection database name.', 1;
IF DB_ID(@CrmDatabase) IS NULL
    THROW 51003, N'CRM database not found on this SQL Server instance.', 1;
IF OBJECT_ID(QUOTENAME(@UserDatabase) + N'.dbo.AspNetUsers', N'U') IS NULL
    THROW 51004, N'AspNetUsers table was not found in the selected User database.', 1;
IF OBJECT_ID(QUOTENAME(@CrmDatabase) + N'.dbo.Customers', N'U') IS NULL
    THROW 51005, N'Customers table was not found in the specified CRM database.', 1;
IF COL_LENGTH(QUOTENAME(@UserDatabase) + N'.dbo.AspNetUsers', N'IsCustomer') IS NULL
   OR COL_LENGTH(QUOTENAME(@UserDatabase) + N'.dbo.AspNetUsers', N'IsSupplier') IS NULL
    THROW 51006, N'Run the UserContextDb migration adding IsCustomer and IsSupplier first.', 1;
IF COL_LENGTH(QUOTENAME(@CrmDatabase) + N'.dbo.Customers', N'RoleType') IS NULL
   OR COL_LENGTH(QUOTENAME(@CrmDatabase) + N'.dbo.Customers', N'IsDeleted') IS NULL
    THROW 51007, N'Customers.RoleType or Customers.IsDeleted does not exist.', 1;

PRINT N'User database: ' + @UserDatabase;
PRINT N'CRM database:  ' + @CrmDatabase;
PRINT CASE WHEN @Apply = 1 THEN N'MODE: APPLY (updates enabled)' ELSE N'MODE: PREVIEW (no updates)' END;

-- Temp tables are retained until the SSMS SQL session ends. Clean up previous runs.
DROP TABLE IF EXISTS #BFPlan;
DROP TABLE IF EXISTS #BFPhoneCounts;
DROP TABLE IF EXISTS #BFPhoneCandidates;
DROP TABLE IF EXISTS #BFCustomers;
DROP TABLE IF EXISTS #BFUsers;

CREATE TABLE #BFUsers (
    UserId nvarchar(450) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY,
    UserName nvarchar(256) NULL,
    PhoneNumber nvarchar(1000) NULL,
    CleanPhone nvarchar(1000) NULL,
    NormalizedPhone nvarchar(30) NULL,
    PersonId uniqueidentifier NULL,
    IsEmployee bit NOT NULL,
    IsCustomer bit NOT NULL,
    IsSupplier bit NOT NULL
);

CREATE TABLE #BFCustomers (
    CustomerId uniqueidentifier NOT NULL PRIMARY KEY,
    RoleType int NOT NULL,
    Mobile nvarchar(1000) NULL,
    CleanPhone nvarchar(1000) NULL,
    NormalizedPhone nvarchar(30) NULL
);

DECLARE @Sql nvarchar(max);
SET @Sql = N'
    INSERT INTO #BFUsers(UserId,UserName,PhoneNumber,PersonId,IsEmployee,IsCustomer,IsSupplier)
    SELECT Id, UserName, LEFT(PhoneNumber,1000), PersonId, IsEmployee, IsCustomer, IsSupplier
    FROM ' + QUOTENAME(@UserDatabase) + N'.dbo.AspNetUsers;
    INSERT INTO #BFCustomers(CustomerId,RoleType,Mobile)
    SELECT Id, CONVERT(int,RoleType), LEFT(Mobile,1000)
    FROM ' + QUOTENAME(@CrmDatabase) + N'.dbo.Customers
    WHERE IsDeleted = 0;';
EXEC sys.sp_executesql @Sql;

-- Convert Persian and Arabic digits to English, then remove harmless separators.
UPDATE #BFUsers SET CleanPhone =
    TRANSLATE(COALESCE(PhoneNumber,N''), N'۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩', N'01234567890123456789');
UPDATE #BFCustomers SET CleanPhone =
    TRANSLATE(COALESCE(Mobile,N''), N'۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩', N'01234567890123456789');

DECLARE @RemoveChars nvarchar(32) =
    N' -+()/.‐–—' + NCHAR(9) + NCHAR(10) + NCHAR(13) + NCHAR(160) + NCHAR(8204);
DECLARE @i int = 1, @Ch nchar(1);
WHILE @i <= LEN(@RemoveChars)
BEGIN
    SET @Ch = SUBSTRING(@RemoveChars,@i,1);
    UPDATE #BFUsers SET CleanPhone = REPLACE(CleanPhone,@Ch,N'');
    UPDATE #BFCustomers SET CleanPhone = REPLACE(CleanPhone,@Ch,N'');
    SET @i += 1;
END;

-- Only accept structurally valid Iranian mobile numbers, in a canonical 09xxxxxxxxx form.
UPDATE #BFUsers
SET NormalizedPhone = CASE
    WHEN LEN(CleanPhone)=11 AND CleanPhone LIKE N'09%' AND CleanPhone NOT LIKE N'%[^0-9]%'
        THEN CleanPhone
    WHEN LEN(CleanPhone)=10 AND CleanPhone LIKE N'9%' AND CleanPhone NOT LIKE N'%[^0-9]%'
        THEN N'0'+CleanPhone
    WHEN LEN(CleanPhone)=12 AND CleanPhone LIKE N'989%' AND CleanPhone NOT LIKE N'%[^0-9]%'
        THEN N'0'+SUBSTRING(CleanPhone,3,10)
    WHEN LEN(CleanPhone)=14 AND CleanPhone LIKE N'00989%' AND CleanPhone NOT LIKE N'%[^0-9]%'
        THEN N'0'+SUBSTRING(CleanPhone,5,10)
    ELSE NULL END;
UPDATE #BFCustomers
SET NormalizedPhone = CASE
    WHEN LEN(CleanPhone)=11 AND CleanPhone LIKE N'09%' AND CleanPhone NOT LIKE N'%[^0-9]%'
        THEN CleanPhone
    WHEN LEN(CleanPhone)=10 AND CleanPhone LIKE N'9%' AND CleanPhone NOT LIKE N'%[^0-9]%'
        THEN N'0'+CleanPhone
    WHEN LEN(CleanPhone)=12 AND CleanPhone LIKE N'989%' AND CleanPhone NOT LIKE N'%[^0-9]%'
        THEN N'0'+SUBSTRING(CleanPhone,3,10)
    WHEN LEN(CleanPhone)=14 AND CleanPhone LIKE N'00989%' AND CleanPhone NOT LIKE N'%[^0-9]%'
        THEN N'0'+SUBSTRING(CleanPhone,5,10)
    ELSE NULL END;

SELECT u.UserId, c.CustomerId, c.RoleType,
       COUNT(*) OVER(PARTITION BY u.UserId) AS CandidateCount
INTO #BFPhoneCandidates
FROM #BFUsers u
INNER JOIN #BFCustomers c ON c.NormalizedPhone = u.NormalizedPhone
WHERE @UseUniqueMobile = 1 AND u.PersonId IS NULL
  AND u.NormalizedPhone IS NOT NULL;

SELECT UserId, MAX(CandidateCount) AS CandidateCount
INTO #BFPhoneCounts
FROM #BFPhoneCandidates GROUP BY UserId;

SELECT m.*,
    CONVERT(bit,CASE WHEN m.RoleType IN (1,3) OR m.OldIsCustomer=1 THEN 1 ELSE 0 END) AS NewIsCustomer,
    CONVERT(bit,CASE WHEN m.RoleType IN (2,3) OR m.OldIsSupplier=1 THEN 1 ELSE 0 END) AS NewIsSupplier,
    CASE
      WHEN m.RoleType IN (1,2,3) THEN N'OK'
      WHEN m.PersonId IS NOT NULL AND m.DirectCustomerId IS NULL THEN N'PersonIdNotFoundOrDeleted'
      WHEN m.DirectCustomerId IS NOT NULL THEN N'InvalidRoleType'
      WHEN m.NormalizedPhone IS NULL THEN N'NoValidMobile'
      WHEN m.PhoneMatchCount > 1 THEN N'AmbiguousMobile'
      WHEN m.PhoneMatchCount = 0 THEN N'NoCRMCustomerForMobile'
      ELSE N'NotMatched'
    END AS ReviewReason
INTO #BFPlan
FROM (
    SELECT u.UserId,u.UserName,u.PersonId,u.NormalizedPhone,u.IsEmployee,
        u.IsCustomer AS OldIsCustomer,u.IsSupplier AS OldIsSupplier,
        d.CustomerId AS DirectCustomerId,
        COALESCE(pc.CandidateCount,0) AS PhoneMatchCount,
        CASE
           WHEN d.CustomerId IS NOT NULL THEN d.CustomerId
           WHEN u.PersonId IS NULL AND p.CandidateCount=1 THEN p.CustomerId
           ELSE NULL END AS MatchedCustomerId,
        CASE
           WHEN d.CustomerId IS NOT NULL THEN d.RoleType
           WHEN u.PersonId IS NULL AND p.CandidateCount=1 THEN p.RoleType
           ELSE NULL END AS RoleType,
        CASE
           WHEN d.CustomerId IS NOT NULL THEN N'PersonId'
           WHEN u.PersonId IS NULL AND p.CandidateCount=1 THEN N'UniqueMobile'
           ELSE N'Unmatched' END AS MatchMethod
    FROM #BFUsers u
    LEFT JOIN #BFCustomers d ON d.CustomerId=u.PersonId
    LEFT JOIN #BFPhoneCandidates p ON p.UserId=u.UserId AND p.CandidateCount=1
    LEFT JOIN #BFPhoneCounts pc ON pc.UserId=u.UserId
) m;

PRINT N'--- Summary: only new TRUE flags are proposed; existing TRUE flags are preserved ---';
SELECT
    MatchMethod,
    IsEmployee,
    COUNT(*) AS TotalUsers,
    SUM(CASE WHEN ReviewReason=N'OK' THEN 1 ELSE 0 END) AS MatchedValidUsers,
    SUM(CASE WHEN ReviewReason=N'OK' AND
                 (OldIsCustomer<>NewIsCustomer OR OldIsSupplier<>NewIsSupplier) THEN 1 ELSE 0 END) AS UsersToUpdate
FROM #BFPlan
GROUP BY MatchMethod,IsEmployee ORDER BY MatchMethod,IsEmployee;

PRINT N'--- Proposed updates (review before changing @Apply to 1) ---';
SELECT UserId,UserName,IsEmployee,MatchMethod,MatchedCustomerId,RoleType,
       OldIsCustomer,NewIsCustomer,OldIsSupplier,NewIsSupplier
FROM #BFPlan
WHERE ReviewReason=N'OK'
  AND (OldIsCustomer<>NewIsCustomer OR OldIsSupplier<>NewIsSupplier)
ORDER BY MatchMethod,IsEmployee,UserName;

PRINT N'--- Unmatched / ambiguous NON-EMPLOYEE accounts (first 200) ---';
SELECT TOP (200) UserId,UserName,PersonId,NormalizedPhone,
       PhoneMatchCount,ReviewReason
FROM #BFPlan
WHERE ReviewReason<>N'OK' AND IsEmployee=0
ORDER BY ReviewReason,UserName;

DECLARE @Expected int = (
  SELECT COUNT(*) FROM #BFPlan
  WHERE ReviewReason=N'OK'
    AND (OldIsCustomer<>NewIsCustomer OR OldIsSupplier<>NewIsSupplier)
);

IF @Apply = 0
BEGIN
    PRINT N'PREVIEW ONLY: no database rows were updated. To apply, set @Apply=1 and rerun after backup.';
END
ELSE
BEGIN
    DECLARE @Affected int = 0;
    BEGIN TRY
        BEGIN TRANSACTION;
        SET @Sql = N'
          UPDATE u
          SET u.IsCustomer = p.NewIsCustomer,
              u.IsSupplier = p.NewIsSupplier
          FROM ' + QUOTENAME(@UserDatabase) + N'.dbo.AspNetUsers u
          INNER JOIN #BFPlan p ON u.Id COLLATE DATABASE_DEFAULT = p.UserId
          WHERE p.ReviewReason=N''OK''
            AND (p.OldIsCustomer<>p.NewIsCustomer OR p.OldIsSupplier<>p.NewIsSupplier)
            AND u.IsCustomer=p.OldIsCustomer
            AND u.IsSupplier=p.OldIsSupplier;
          SET @RowsChanged=@@ROWCOUNT;';
        EXEC sys.sp_executesql @Sql,N'@RowsChanged int OUTPUT',@RowsChanged=@Affected OUTPUT;
        IF @Affected<>@Expected
            THROW 51008, N'Concurrent user flag changes detected; transaction rolled back. Preview and retry.', 1;
        COMMIT TRANSACTION;
        PRINT N'APPLIED: updated '+CONVERT(nvarchar(20),@Affected)+N' AspNetUsers rows. IsEmployee was untouched.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
