/*
  کنترل کیفیت قدیمی -> بررسی اقلام فاکتور
  اجرای مستقیم در SQL Server Management Studio (SSMS)

  @Apply = 0 : فقط پیش نمایش و گزارش سفارش‌های مغایرت‌دار، بدون تغییر دیتابیس
  @Apply = 1 : اجرای بک‌فیل واقعی (فقط موارد قطعا بدون مغایرت)

  قبل از اجرای واقعی بک‌آپ کامل دیتابیس CRM تهیه کنید.
  این اسکریپت، وضعیت‌های مغایرت‌دار، عدم پاسخ و شماره نامعتبر را به اقلام منتقل نمی‌کند.
  هر قلمی که در جدول جدید رکورد دارد (حتی حذف نرم) دست‌نخورده می‌ماند.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply bit = 0; -- برای اجرای واقعی فقط این عدد را از 0 به 1 تغییر دهید
DECLARE @BackupConfirmed bit = 0; -- بعد از تهیه و تست بکاپ کامل، 1 کنید
-- یادداشت‌های قدیمی مستقل از نتیجه اقلام، در همان تراکنش ثبت می‌شوند.

IF OBJECT_ID(N'dbo.QualityControls', N'U') IS NULL
   OR OBJECT_ID(N'dbo.QualityControlAnswers', N'U') IS NULL
   OR OBJECT_ID(N'dbo.QualityControlItemReviews', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Deals', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Factors', N'U') IS NULL
   OR OBJECT_ID(N'dbo.FactorItems', N'U') IS NULL
   OR OBJECT_ID(N'dbo.OrderHeads', N'U') IS NULL
   OR OBJECT_ID(N'dbo.QualityControlCustomerNotes', N'U') IS NULL
BEGIN
    THROW 51001, 'Required quality-control tables are missing. Check the selected CRM database.', 1;
END;

-- در صورتی که بک‌فیل قدیمی به شکل مایگریشن اجرا شده باشد، اجرای دوباره را متوقف کن.
-- چون نسخه اولیه آن ممکن است موارد مغایرت‌دار را به غلط بدون مغایرت ثبت کرده باشد.
DECLARE @PreviousMigrationApplied bit = 0;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql
        N'SELECT @Found = CASE WHEN EXISTS
          (SELECT 1 FROM dbo.__EFMigrationsHistory
           WHERE MigrationId = N''20261119190000_BackfillLegacyQualityControlItems'')
          THEN 1 ELSE 0 END',
        N'@Found bit OUTPUT',
        @Found = @PreviousMigrationApplied OUTPUT;
END;

IF @Apply = 1 AND @BackupConfirmed <> 1
BEGIN
    THROW 51003, 'Full CRM backup confirmation required before apply.', 1;
END;

IF @Apply = 1 AND @PreviousMigrationApplied = 1
BEGIN
    THROW 51002, 'Legacy backfill migration already ran. Inspect migrated records before doing more writes.', 1;
END;

-- 1) پیش‌نمایش اقلامی که امکان ثبت دارند؛ هیچ رکورد جدیدی ساخته نمی‌شود.
IF OBJECT_ID(N'tempdb..#Candidates') IS NOT NULL DROP TABLE #Candidates;

SELECT
    fi.Id AS FactorItemId,
    qc.Id AS LegacyQualityControlId,
    qc.DealId,
    d.SerialNumber AS DealNumber,
    f.Id AS FactorId,
    f.SerialNumber AS FactorNumber,
    qc.CreateDate, qc.ModifyDate,
    qc.CreateDateTxt, qc.ModifyDateTxt,
    qc.UserCreatorID, qc.UserCreator,
    qc.UserModifyID, qc.UserModify,
    qc.[Description] AS LegacyDescription,
    COALESCE(f.CustomerId, d.CustomerId) AS CustomerId,
    ROW_NUMBER() OVER (PARTITION BY fi.Id ORDER BY qc.CreateDate DESC, qc.Id) AS CandidateRank
INTO #Candidates
FROM dbo.QualityControls AS qc
INNER JOIN dbo.Deals AS d ON d.Id = qc.DealId
INNER JOIN dbo.Factors AS f ON f.DealId = d.Id
INNER JOIN dbo.FactorItems AS fi ON fi.factorId = f.Id
WHERE qc.IsDeleted = 0
  AND d.IsDeleted = 0 AND d.FinancialStatus = 1
  AND f.IsDeleted = 0 AND f.[status] = 2 AND f.Returned = 0
  AND fi.IsDeleted = 0
  AND COALESCE(f.CustomerId, d.CustomerId) IS NOT NULL
  -- اگر چند نتیجه قدیمی متناقض برای یک پرونده باشد، بدون مغایرت تلقی نکن:
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.QualityControls AS other
      WHERE other.DealId = qc.DealId AND other.IsDeleted = 0
        AND
        (
            other.[Status] = 2
            OR EXISTS
            (
                SELECT 1 FROM dbo.QualityControlAnswers AS conflicting
                WHERE conflicting.QualityControlId = other.Id
                  AND conflicting.IsDeleted = 0 AND conflicting.Answer = 1
            )
        )
  )
  -- فقط کنترل کیفیت «بدون مغایرت» یا رکورد قدیمی دارای پاسخ‌های بدون مغایرت:
  AND
  (
      qc.[Status] = 1
      OR
      (
          qc.[Status] IS NULL
          AND EXISTS
          (
              SELECT 1 FROM dbo.QualityControlAnswers AS a
              WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0
          )
      )
  )
  -- هر مغایرت ثبت‌شده در پاسخ‌ها، حتی با Status=1، مانع انتقال کل سفارش می‌شود:
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.QualityControlAnswers AS a
      WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0 AND a.Answer = 1
  )
  -- رکورد جدیدِ موجود (فعال یا حذف نرم) را تغییر نده:
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.QualityControlItemReviews AS ir
      WHERE ir.FactorItemId = fi.Id
  );

-- یک یادداشت تاریخی به‌ازای هر کنترل کیفیت قدیمی، نه به‌ازای هر قلم.
-- اگر اقلام قبلاً منتقل شده‌اند، توضیحات آن‌ها همچنان باید بازیابی شوند.
DROP TABLE IF EXISTS #LegacyNotes;
SELECT qc.Id AS LegacyQualityControlId,
       COALESCE(owner.CustomerId, d.CustomerId) AS CustomerId,
       qc.[Description] AS LegacyDescription,
       qc.CreateDate, qc.ModifyDate, qc.CreateDateTxt, qc.ModifyDateTxt,
       qc.UserCreatorID, qc.UserCreator, qc.UserModifyID, qc.UserModify
INTO #LegacyNotes
FROM dbo.QualityControls AS qc
INNER JOIN dbo.Deals AS d ON d.Id = qc.DealId AND d.IsDeleted = 0
OUTER APPLY
(
    SELECT TOP (1) f.CustomerId
    FROM dbo.Factors AS f
    WHERE f.DealId = d.Id AND f.IsDeleted = 0 AND f.CustomerId IS NOT NULL
    ORDER BY f.CreateDate DESC, f.Id
) AS owner
WHERE qc.IsDeleted = 0
  AND NULLIF(LTRIM(RTRIM(qc.[Description])),N'') IS NOT NULL
  AND COALESCE(owner.CustomerId, d.CustomerId) IS NOT NULL
  AND
  (
      qc.[Status] = 1
      OR (qc.[Status] IS NULL AND EXISTS
          (SELECT 1 FROM dbo.QualityControlAnswers AS a
           WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0))
  )
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.QualityControls AS other
      WHERE other.DealId = qc.DealId AND other.IsDeleted = 0
        AND (other.[Status] = 2 OR EXISTS
             (SELECT 1 FROM dbo.QualityControlAnswers AS a
              WHERE a.QualityControlId = other.Id AND a.IsDeleted = 0 AND a.Answer = 1))
  )
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.QualityControlAnswers AS a
      WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0 AND a.Answer = 1
  )
  AND EXISTS (SELECT 1 FROM dbo.Customers AS c
              WHERE c.Id = COALESCE(owner.CustomerId, d.CustomerId));

SELECT COUNT(*) AS LegacyNotesEligible,
       SUM(CASE WHEN EXISTS
           (SELECT 1 FROM dbo.QualityControlCustomerNotes AS n
            WHERE n.LegacyQualityControlId = ln.LegacyQualityControlId)
           THEN 1 ELSE 0 END) AS LegacyNotesAlreadyImported
FROM #LegacyNotes AS ln;

SELECT
    CASE WHEN @Apply = 1 THEN N'اجرای واقعی' ELSE N'فقط پیش‌نمایش' END AS ExecutionMode,
    @PreviousMigrationApplied AS OldMigrationAlreadyApplied,
    COUNT(DISTINCT DealId) AS DealsToBackfill,
    COUNT(DISTINCT FactorId) AS FactorsToBackfill,
    COUNT(*) AS ItemsToInsert
FROM #Candidates WHERE CandidateRank = 1;

SELECT DealNumber AS [شماره پرونده],
       COUNT(DISTINCT FactorId) AS [تعداد فاکتور],
       COUNT(*) AS [تعداد قلم برای ثبت بدون مغایرت]
FROM #Candidates WHERE CandidateRank = 1
GROUP BY DealNumber
ORDER BY DealNumber DESC;

-- 2) لیست سفارش/حواله‌هایی که در کنترل کیفیت قدیمی مغایرت داشتند؛ فقط گزارش.
-- هر Deal ممکن است چند OrderHead داشته باشد، پس همه شماره‌ها نمایش داده می‌شوند.
;WITH DiscrepantOldQc AS
(
    SELECT qc.Id, qc.DealId, qc.[Status], qc.[Description],
           qc.CreateDate,
           (
               SELECT COUNT(*) FROM dbo.QualityControlAnswers AS a
               WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0 AND a.Answer = 1
           ) AS PositiveAnswerCount
    FROM dbo.QualityControls AS qc
    WHERE qc.IsDeleted = 0
      AND
      (
          qc.[Status] = 2
          OR EXISTS
          (
              SELECT 1 FROM dbo.QualityControlAnswers AS a
              WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0 AND a.Answer = 1
          )
      )
)
SELECT
    d.SerialNumber AS [شماره پرونده],
    oh.OrderNumber AS [شماره حواله / سفارش],
    d.Title AS [عنوان پرونده],
    d.CustomerName AS [نام مشتری],
    old.[Status] AS [وضعیت قدیمی],
    old.PositiveAnswerCount AS [تعداد پاسخ دارای مغایرت],
    old.[Description] AS [شرح کنترل کیفیت قدیمی],
    old.CreateDate AS [تاریخ بررسی قدیمی],
    d.Id AS DealId,
    oh.Id AS OrderHeadId,
    old.Id AS LegacyQualityControlId
FROM DiscrepantOldQc AS old
INNER JOIN dbo.Deals AS d ON d.Id = old.DealId AND d.IsDeleted = 0
LEFT JOIN dbo.OrderHeads AS oh ON oh.DealId = d.Id AND oh.IsDeleted = 0
ORDER BY d.SerialNumber DESC, oh.OrderNumber DESC;

-- 3) ثبت واقعی فقط با @Apply=1؛ در تراکنش، با محافظت از رکوردهای موجود.
IF @Apply = 1
BEGIN
    IF EXISTS (SELECT 1 FROM #LegacyNotes WHERE LEN(LegacyDescription) > 4000)
        THROW 51004, 'Legacy description exceeds 4000 characters; no truncation is permitted.', 1;
    DECLARE @Inserted int = 0;
    DECLARE @NotesInserted int = 0;
    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.QualityControlItemReviews
        (
            Id, FactorItemId, [Status], [Description],
            CreateDate, ModifyDate, CreateDateTxt, ModifyDateTxt,
            IsDeleted, UserCreatorID, UserCreator, UserModifyID, UserModify, CommentText
        )
        SELECT
            NEWID(), c.FactorItemId, 1, NULL,
            c.CreateDate, COALESCE(c.ModifyDate, c.CreateDate),
            c.CreateDateTxt, COALESCE(c.ModifyDateTxt, c.CreateDateTxt),
            0, c.UserCreatorID, c.UserCreator, c.UserModifyID, c.UserModify,
            CONCAT(N'بک‌فیل کنترل کیفیت قدیمی بدون مغایرت؛ شناسه اصلی: ',
                   CONVERT(nvarchar(36), c.LegacyQualityControlId))
        FROM #Candidates AS c
        WHERE c.CandidateRank = 1 AND NOT EXISTS
        (
            SELECT 1
            FROM dbo.QualityControlItemReviews AS ir WITH (UPDLOCK, HOLDLOCK)
            WHERE ir.FactorItemId = c.FactorItemId
        );

        SET @Inserted = @@ROWCOUNT;

        -- حفظ توضیحات در یادداشت‌های مشتری؛ بدون تکثیر به تعداد اقلام.
        -- شاخص یکتای LegacyQualityControlId از ورود تکراری جلوگیری می‌کند.
        INSERT INTO dbo.QualityControlCustomerNotes
        (
            Id, CustomerId, [Description], [Source],
            CallRecordingId, LegacyQualityControlId, OriginalTranscript,
            CreateDate, ModifyDate, CreateDateTxt, ModifyDateTxt,
            IsDeleted, UserCreatorID, UserCreator, UserModifyID, UserModify, CommentText
        )
        SELECT NEWID(), ln.CustomerId, ln.LegacyDescription, 2,
               NULL, ln.LegacyQualityControlId, NULL,
               ln.CreateDate, ln.ModifyDate, ln.CreateDateTxt, ln.ModifyDateTxt,
               0, ln.UserCreatorID, ln.UserCreator, ln.UserModifyID, ln.UserModify,
               N'انتقال یادداشت کنترل کیفیت قدیمی بدون مغایرت'
        FROM #LegacyNotes AS ln
        WHERE NOT EXISTS
        (
            SELECT 1 FROM dbo.QualityControlCustomerNotes AS note WITH (UPDLOCK,HOLDLOCK)
            WHERE note.LegacyQualityControlId = ln.LegacyQualityControlId
        );
        SET @NotesInserted = @@ROWCOUNT;
        COMMIT TRANSACTION;

        SELECT N'بک‌فیل با موفقیت اجرا شد' AS [نتیجه], @Inserted AS [تعداد اقلام ثبت‌شده], @NotesInserted AS [تعداد یادداشت‌های منتقل‌شده];
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
ELSE
BEGIN
    SELECT N'این اجرا فقط پیش‌نمایش بود؛ برای ثبت واقعی @Apply را 1 کنید.' AS [نتیجه];
END;
DROP TABLE IF EXISTS #LegacyNotes;
DROP TABLE IF EXISTS #Candidates;
