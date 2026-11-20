/*
  SAFE legacy QC backfill: NO DISCREPANCY + CUSTOMER NOTES
  SQL Server 2019 / SSMS; select the CRM database before running.
  Default @Apply=0 gives preflight and postflight counts WITHOUT writes.
  For the real run: first take a verified FULL backup, then set both
  @Apply=1 and @BackupConfirmed=1. Counts printed for every run.

  Item source: one safe legacy review per DealId -> all eligible FactorItemIds.
  Only Status=1 or NULL with an actual saved question, AND NO historical
  positive answer or Status=2 anywhere in the same deal.
  Target: QualityControlItemReviews.Status=1; existing rows never touched.
  Note source: each safe old QC with a nonempty Description (once per QC,
  not once per invoice line), target QualityControlCustomerNotes.Source=2.
  Existing notes (even soft-deleted) never touched.
  No data from discrepant deals / no-answer / invalid-number is migrated.
  Does NOT delete or update any original legacy QC row or OrderHead.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply bit = 0; -- 0 = report only; 1 = execute real inserts
DECLARE @BackupConfirmed bit = 0; -- set to 1 ONLY after verifying a full CRM backup
DECLARE @ExpectedDatabase sysname = N'SCRM'; -- change explicitly if your CRM DB is named differently
DECLARE @PriorMigrationApplied bit = 0;

IF DB_NAME() <> @ExpectedDatabase
    THROW 51101, 'Unexpected database. Select the CRM database and check @ExpectedDatabase.', 1;

IF OBJECT_ID(N'dbo.QualityControls', N'U') IS NULL
 OR OBJECT_ID(N'dbo.QualityControlAnswers', N'U') IS NULL
 OR OBJECT_ID(N'dbo.QualityControlItemReviews', N'U') IS NULL
 OR OBJECT_ID(N'dbo.QualityControlCustomerNotes', N'U') IS NULL
 OR OBJECT_ID(N'dbo.Deals', N'U') IS NULL
 OR OBJECT_ID(N'dbo.Factors', N'U') IS NULL
 OR OBJECT_ID(N'dbo.FactorItems', N'U') IS NULL
 OR OBJECT_ID(N'dbo.OrderHeads', N'U') IS NULL
 OR OBJECT_ID(N'dbo.Customers', N'U') IS NULL
    THROW 51102, 'Required QC tables are missing. Apply structural migrations first.', 1;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
BEGIN
    SELECT @PriorMigrationApplied =
        CASE WHEN EXISTS
        (
            SELECT 1 FROM dbo.__EFMigrationsHistory
            WHERE MigrationId = N'20261119190000_BackfillLegacyQualityControlItems'
        ) THEN 1 ELSE 0 END;
END;

DROP TABLE IF EXISTS #SafeLegacy;
DROP TABLE IF EXISTS #EligibleItems;
DROP TABLE IF EXISTS #LegacyNotes;
DROP TABLE IF EXISTS #InsertedItems;
DROP TABLE IF EXISTS #InsertedNotes;
DROP TABLE IF EXISTS #ItemAfter;
DROP TABLE IF EXISTS #NoteAfter;
DROP TABLE IF EXISTS #DealProgress;

-- A legacy deal may have only one QC in today's mapping. Keep deterministic
-- selection anyway; any conflicting old result excludes the WHOLE deal.
SELECT qc.Id AS LegacyQualityControlId,
       qc.DealId, d.SerialNumber AS DealNumber, d.Title AS DealTitle,
       d.CustomerId AS DealCustomerId, d.FinancialStatus,
       qc.[Description] AS LegacyDescription,
       qc.CreateDate, qc.ModifyDate,
       qc.CreateDateTxt, qc.ModifyDateTxt,
       qc.UserCreatorID, qc.UserCreator, qc.UserModifyID, qc.UserModify,
       ROW_NUMBER() OVER
           (PARTITION BY qc.DealId ORDER BY qc.CreateDate DESC, qc.Id DESC)
           AS SourceRank
INTO #SafeLegacy
FROM dbo.QualityControls AS qc
INNER JOIN dbo.Deals AS d ON d.Id = qc.DealId
WHERE qc.IsDeleted = 0 AND d.IsDeleted = 0
  AND
  (
      qc.[Status] = 1
      OR
      (
          qc.[Status] IS NULL
          AND EXISTS
          (
              SELECT 1 FROM dbo.QualityControlAnswers AS ans
              WHERE ans.QualityControlId = qc.Id AND ans.IsDeleted = 0
          )
      )
  )
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.QualityControls AS other
      WHERE other.DealId = qc.DealId AND other.IsDeleted = 0
        AND
        (
            other.[Status] = 2
            OR EXISTS
            (
                SELECT 1 FROM dbo.QualityControlAnswers AS positive
                WHERE positive.QualityControlId = other.Id
                  AND positive.IsDeleted = 0 AND positive.Answer = 1
            )
        )
  );

-- The new QC queue works on eligible, approved invoice items. Orders are
-- associated with a Deal, NOT mapped individually to FactorItem rows.
SELECT s.LegacyQualityControlId, s.DealId, s.DealNumber,
       f.Id AS FactorId, f.SerialNumber AS FactorNumber,
       fi.Id AS FactorItemId,
       s.CreateDate, s.ModifyDate, s.CreateDateTxt, s.ModifyDateTxt,
       s.UserCreatorID, s.UserCreator, s.UserModifyID, s.UserModify
INTO #EligibleItems
FROM #SafeLegacy AS s
INNER JOIN dbo.Factors AS f ON f.DealId = s.DealId
    AND f.IsDeleted = 0 AND f.[status] = 2 AND f.Returned = 0
INNER JOIN dbo.FactorItems AS fi ON fi.factorId = f.Id AND fi.IsDeleted = 0
INNER JOIN dbo.Customers AS customer
    ON customer.Id = COALESCE(f.CustomerId, s.DealCustomerId)
    AND customer.IsDeleted = 0
WHERE s.SourceRank = 1 AND s.FinancialStatus = 1;

IF EXISTS
(
    SELECT 1 FROM #EligibleItems GROUP BY FactorItemId HAVING COUNT(*) > 1
)
    THROW 51103, 'Duplicate invoice-item candidates: no inserts made.', 1;

-- Notes are customer-level. If the deal points to multiple effective
-- invoice customers, do not guess who owns the old deal-level comment.
-- A deal customer is used only if it agrees with the single invoice owner
-- or if no eligible invoices exist. Missing/ambiguous owners are reported.
SELECT s.LegacyQualityControlId, s.DealId, s.DealNumber,
       s.LegacyDescription, s.CreateDate, s.ModifyDate,
       s.CreateDateTxt, s.ModifyDateTxt,
       s.UserCreatorID, s.UserCreator, s.UserModifyID, s.UserModify,
       CASE
           WHEN owners.DistinctOwners > 1 THEN CAST(NULL AS uniqueidentifier)
           WHEN owners.DistinctOwners = 1
                AND s.DealCustomerId IS NOT NULL
                AND s.DealCustomerId <> TRY_CONVERT(uniqueidentifier, owners.SingleCustomerIdText)
                THEN CAST(NULL AS uniqueidentifier)
           WHEN owners.DistinctOwners = 1 THEN TRY_CONVERT(uniqueidentifier, owners.SingleCustomerIdText)
           ELSE s.DealCustomerId
       END AS ProposedCustomerId
INTO #LegacyNotes
FROM #SafeLegacy AS s
OUTER APPLY
(
    SELECT COUNT(DISTINCT COALESCE(f.CustomerId, s.DealCustomerId))
               AS DistinctOwners,
           MIN(CONVERT(char(36), COALESCE(f.CustomerId, s.DealCustomerId)))
               AS SingleCustomerIdText
    FROM dbo.Factors AS f
    INNER JOIN dbo.FactorItems AS fi ON fi.factorId = f.Id
        AND fi.IsDeleted = 0
    WHERE f.DealId = s.DealId AND s.FinancialStatus = 1
      AND f.IsDeleted = 0 AND f.[status] = 2 AND f.Returned = 0
) AS owners
WHERE NULLIF(LTRIM(RTRIM(s.LegacyDescription)), N'') IS NOT NULL;

-- Detect non-existent/deleted customer and mark as unresolved, without
-- discarding the original description. Historical descriptions are NOT
-- copied into the per-item discrepancy field.
CREATE TABLE #InsertedItems (FactorItemId uniqueidentifier NOT NULL PRIMARY KEY);
CREATE TABLE #InsertedNotes (LegacyQualityControlId uniqueidentifier NOT NULL PRIMARY KEY);

-- Before execution. These numbers tell you what WOULD be inserted.
SELECT DB_NAME() AS DatabaseName,
       CASE WHEN @Apply = 1 THEN N'اجرای واقعی' ELSE N'فقط پیش نمایش' END AS ExecutionMode,
       @PriorMigrationApplied AS OldMigrationAlreadyApplied,
       (SELECT COUNT(*) FROM #SafeLegacy) AS EligibleLegacyReviews,
       (SELECT COUNT(DISTINCT DealId) FROM #SafeLegacy) AS EligibleLegacyDeals,
       (SELECT COUNT(DISTINCT DealId) FROM #EligibleItems) AS DealsWithEligibleItems,
       (SELECT COUNT(DISTINCT FactorId) FROM #EligibleItems) AS EligibleInvoices,
       (SELECT COUNT(*) FROM dbo.OrderHeads AS oh
        WHERE oh.IsDeleted = 0 AND EXISTS
            (SELECT 1 FROM #EligibleItems AS ei WHERE ei.DealId = oh.DealId)
       ) AS RelatedOrderHeads,
       (SELECT COUNT(*) FROM #EligibleItems) AS EligibleInvoiceItems,
       (SELECT COUNT(*) FROM #EligibleItems AS e
        WHERE NOT EXISTS (SELECT 1 FROM dbo.QualityControlItemReviews AS r
                          WHERE r.FactorItemId = e.FactorItemId)) AS PendingInvoiceItemsBefore,
       (SELECT COUNT(*) FROM #LegacyNotes) AS LegacyDescriptionsWithText,
       (SELECT COUNT(*) FROM #LegacyNotes AS n
        INNER JOIN dbo.Customers AS c
            ON c.Id = n.ProposedCustomerId AND c.IsDeleted = 0
        WHERE LEN(n.LegacyDescription) <= 4000
          AND NOT EXISTS (SELECT 1 FROM dbo.QualityControlCustomerNotes AS old
                          WHERE old.LegacyQualityControlId = n.LegacyQualityControlId)
       ) AS PendingCustomerNotesBefore;

-- Row-level preview of each safe deal. No changes have yet been made.
SELECT s.DealNumber AS [شماره پرونده],
       s.DealId,
       COUNT(DISTINCT e.FactorId) AS [فاکتورهای واجد شرایط],
       COUNT(DISTINCT e.FactorItemId) AS [اقلام واجد شرایط],
       COUNT(DISTINCT CASE WHEN r.Id IS NULL THEN e.FactorItemId END)
           AS [اقلام نیازمند انتقال],
       COUNT(DISTINCT CASE WHEN r.Id IS NOT NULL THEN e.FactorItemId END)
           AS [اقلام دارای نتیجه قبلی]
FROM (SELECT DISTINCT DealId, DealNumber FROM #SafeLegacy) AS s
LEFT JOIN #EligibleItems AS e ON e.DealId = s.DealId
LEFT JOIN dbo.QualityControlItemReviews AS r ON r.FactorItemId = e.FactorItemId
GROUP BY s.DealNumber, s.DealId
ORDER BY s.DealNumber DESC;

IF @Apply = 1
BEGIN
    IF @BackupConfirmed <> 1
        THROW 51104, 'Verify a FULL CRM backup; then set @BackupConfirmed=1.', 1;

    -- An old iteration of this migration could wrongly label discrepant
    -- deals as NoDiscrepancy. Stop instead of concealing that situation.
    IF @PriorMigrationApplied = 1
        THROW 51105, 'Old backfill migration was already applied. Audit old migrated rows first.', 1;

    IF EXISTS
    (
        SELECT 1 FROM #LegacyNotes AS n
        LEFT JOIN dbo.Customers AS c
            ON c.Id = n.ProposedCustomerId AND c.IsDeleted = 0
        WHERE c.Id IS NOT NULL AND LEN(n.LegacyDescription) > 4000
          AND NOT EXISTS (SELECT 1 FROM dbo.QualityControlCustomerNotes AS old
                          WHERE old.LegacyQualityControlId = n.LegacyQualityControlId)
    )
        THROW 51106, 'Old note exceeds 4000 characters. Review it before applying.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @LockResult int;
        EXEC @LockResult = sys.sp_getapplock
            @Resource = N'QC_LegacyNoDiscrepancyItemsAndNotes',
            @LockMode = N'Exclusive',
            @LockOwner = N'Transaction',
            @LockTimeout = 15000;
        IF @LockResult < 0
            THROW 51107, 'Backfill is running in another session. Try later.', 1;

        INSERT INTO dbo.QualityControlItemReviews
        (
            Id, FactorItemId, [Status], [Description],
            CreateDate, ModifyDate, CreateDateTxt, ModifyDateTxt,
            IsDeleted, UserCreatorID, UserCreator,
            UserModifyID, UserModify, CommentText
        )
        OUTPUT INSERTED.FactorItemId INTO #InsertedItems(FactorItemId)
        SELECT NEWID(), e.FactorItemId, 1, NULL,
               e.CreateDate, COALESCE(e.ModifyDate, e.CreateDate),
               e.CreateDateTxt, COALESCE(e.ModifyDateTxt, e.CreateDateTxt),
               0, e.UserCreatorID, e.UserCreator,
               e.UserModifyID, e.UserModify,
               CONCAT(N'بک فیل بدون مغایرت؛ کنترل کیفیت قدیمی: ',
                      CONVERT(nvarchar(36), e.LegacyQualityControlId))
        FROM #EligibleItems AS e
        INNER JOIN dbo.QualityControls AS qc
            ON qc.Id = e.LegacyQualityControlId AND qc.IsDeleted = 0
        INNER JOIN dbo.Deals AS d
            ON d.Id = e.DealId AND d.IsDeleted = 0 AND d.FinancialStatus = 1
        INNER JOIN dbo.Factors AS f
            ON f.Id = e.FactorId AND f.DealId = d.Id
            AND f.IsDeleted = 0 AND f.[status] = 2 AND f.Returned = 0
        INNER JOIN dbo.FactorItems AS fi
            ON fi.Id = e.FactorItemId AND fi.factorId = f.Id AND fi.IsDeleted = 0
        INNER JOIN dbo.Customers AS c
            ON c.Id = COALESCE(f.CustomerId, d.CustomerId)
            AND c.IsDeleted = 0
        WHERE
          (qc.[Status] = 1 OR (qc.[Status] IS NULL AND EXISTS
              (SELECT 1 FROM dbo.QualityControlAnswers AS ans
               WHERE ans.QualityControlId = qc.Id AND ans.IsDeleted = 0)))
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.QualityControls AS other
              WHERE other.DealId = d.Id AND other.IsDeleted = 0
                AND (other.[Status] = 2 OR EXISTS
                    (SELECT 1 FROM dbo.QualityControlAnswers AS pos
                     WHERE pos.QualityControlId = other.Id
                       AND pos.IsDeleted = 0 AND pos.Answer = 1))
          )
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.QualityControlItemReviews AS old
                  WITH (UPDLOCK, HOLDLOCK)
              WHERE old.FactorItemId = e.FactorItemId
          );

        INSERT INTO dbo.QualityControlCustomerNotes
        (
            Id, CustomerId, [Description], [Source],
            CallRecordingId, LegacyQualityControlId, OriginalTranscript,
            CreateDate, ModifyDate, CreateDateTxt, ModifyDateTxt,
            IsDeleted, UserCreatorID, UserCreator,
            UserModifyID, UserModify, CommentText
        )
        OUTPUT INSERTED.LegacyQualityControlId
            INTO #InsertedNotes(LegacyQualityControlId)
        SELECT NEWID(), n.ProposedCustomerId, n.LegacyDescription, 2,
               NULL, n.LegacyQualityControlId, NULL,
               n.CreateDate, n.ModifyDate, n.CreateDateTxt, n.ModifyDateTxt,
               0, n.UserCreatorID, n.UserCreator,
               n.UserModifyID, n.UserModify,
               CONCAT(N'بک فیل توضیحات قدیمی بدون مغایرت؛ پرونده: ',
                      CONVERT(nvarchar(36), n.DealId))
        FROM #LegacyNotes AS n
        INNER JOIN dbo.Customers AS c
            ON c.Id = n.ProposedCustomerId AND c.IsDeleted = 0
        INNER JOIN dbo.QualityControls AS qc
            ON qc.Id = n.LegacyQualityControlId AND qc.IsDeleted = 0
        WHERE LEN(n.LegacyDescription) <= 4000
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.QualityControlCustomerNotes AS old
                  WITH (UPDLOCK, HOLDLOCK)
              WHERE old.LegacyQualityControlId = n.LegacyQualityControlId
          )
          AND
          (qc.[Status] = 1 OR (qc.[Status] IS NULL AND EXISTS
              (SELECT 1 FROM dbo.QualityControlAnswers AS ans
               WHERE ans.QualityControlId = qc.Id AND ans.IsDeleted = 0)))
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.QualityControls AS other
              WHERE other.DealId = n.DealId AND other.IsDeleted = 0
                AND (other.[Status] = 2 OR EXISTS
                    (SELECT 1 FROM dbo.QualityControlAnswers AS pos
                     WHERE pos.QualityControlId = other.Id
                       AND pos.IsDeleted = 0 AND pos.Answer = 1))
          );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

-- Re-query the database AFTER COMMIT (or unchanged in preview mode).
-- Existing conflicting/soft-deleted QC reviews are NOT overwritten.
SELECT e.DealId, e.DealNumber, e.FactorId, e.FactorItemId,
       r.Id AS ExistingReviewId, r.[Status] AS ReviewStatus, r.IsDeleted,
       CASE WHEN r.Id IS NULL THEN 1 ELSE 0 END AS IsPending,
       CASE WHEN r.Id IS NOT NULL AND r.IsDeleted = 0 AND r.[Status] = 1
            THEN 1 ELSE 0 END AS IsNoDiscrepancy,
       CASE WHEN r.Id IS NOT NULL AND (r.IsDeleted = 1 OR r.[Status] <> 1)
            THEN 1 ELSE 0 END AS NeedsManualReview
INTO #ItemAfter
FROM #EligibleItems AS e
LEFT JOIN dbo.QualityControlItemReviews AS r ON r.FactorItemId = e.FactorItemId;

SELECT n.LegacyQualityControlId, n.DealId, n.DealNumber,
       n.ProposedCustomerId, n.LegacyDescription,
       note.Id AS ExistingNoteId, note.IsDeleted,
       CASE WHEN note.Id IS NULL AND c.Id IS NOT NULL
                     AND LEN(n.LegacyDescription) <= 4000 THEN 1 ELSE 0 END AS IsPending,
       CASE WHEN note.Id IS NULL AND
                    (c.Id IS NULL OR LEN(n.LegacyDescription) > 4000) THEN 1 ELSE 0 END AS IsBlocked
INTO #NoteAfter
FROM #LegacyNotes AS n
LEFT JOIN dbo.Customers AS c ON c.Id = n.ProposedCustomerId AND c.IsDeleted = 0
LEFT JOIN dbo.QualityControlCustomerNotes AS note
    ON note.LegacyQualityControlId = n.LegacyQualityControlId;

SELECT s.DealId, s.DealNumber,
       COALESCE(items.TotalItems, 0) AS EligibleItems,
       COALESCE(items.PendingItems, 0) AS PendingItems,
       COALESCE(items.ConflictingItems, 0) AS ConflictingItems,
       COALESCE(notes.PendingNotes, 0) AS PendingNotes,
       COALESCE(notes.BlockedNotes, 0) AS BlockedNotes
INTO #DealProgress
FROM (SELECT DISTINCT DealId, DealNumber FROM #SafeLegacy) AS s
OUTER APPLY
(
    SELECT COUNT(*) AS TotalItems,
           COALESCE(SUM(e.IsPending), 0) AS PendingItems,
           COALESCE(SUM(e.NeedsManualReview), 0) AS ConflictingItems
    FROM #ItemAfter AS e WHERE e.DealId = s.DealId
) AS items
OUTER APPLY
(
    SELECT COALESCE(SUM(n.IsPending), 0) AS PendingNotes,
           COALESCE(SUM(n.IsBlocked), 0) AS BlockedNotes
    FROM #NoteAfter AS n WHERE n.DealId = s.DealId
) AS notes;

-- Final totals: exact numbers of inserted / already complete / remaining.
SELECT DB_NAME() AS DatabaseName,
       CASE WHEN @Apply = 1 THEN N'اجرای واقعی' ELSE N'فقط پیش نمایش' END AS ExecutionMode,
       @PriorMigrationApplied AS OldMigrationAlreadyApplied,
       (SELECT COUNT(*) FROM #SafeLegacy) AS LegacyNoDiscrepancyReviews,
       (SELECT COUNT(*) FROM #DealProgress) AS LegacyNoDiscrepancyDeals,
       (SELECT COUNT(*) FROM #DealProgress WHERE EligibleItems > 0)
            AS DealsWithEligibleItems,
       (SELECT COUNT(*) FROM #DealProgress
        WHERE EligibleItems > 0 AND PendingItems = 0
          AND ConflictingItems = 0 AND PendingNotes = 0 AND BlockedNotes = 0)
            AS DealsCompleted,
       (SELECT COUNT(*) FROM #DealProgress
        WHERE EligibleItems > 0 AND ConflictingItems = 0 AND BlockedNotes = 0
          AND (PendingItems > 0 OR PendingNotes > 0))
            AS DealsStillPending,
       (SELECT COUNT(*) FROM #DealProgress
        WHERE EligibleItems = 0 OR ConflictingItems > 0 OR BlockedNotes > 0)
            AS DealsNeedingReview,
       (SELECT COUNT(*) FROM #EligibleItems) AS TotalEligibleItems,
       (SELECT COUNT(*) FROM #InsertedItems) AS ItemsInsertedThisRun,
       (SELECT COUNT(*) FROM #ItemAfter WHERE IsNoDiscrepancy = 1)
            AS ItemsAlreadyOrNowNoDiscrepancy,
       (SELECT COUNT(*) FROM #ItemAfter WHERE IsPending = 1)
            AS ItemsRemainingToInsert,
       (SELECT COUNT(*) FROM #ItemAfter WHERE NeedsManualReview = 1)
            AS ItemsExistingWithDifferentOrDeletedResult,
       (SELECT COUNT(*) FROM #LegacyNotes) AS LegacyDescriptionsWithText,
       (SELECT COUNT(*) FROM #InsertedNotes) AS NotesInsertedThisRun,
       (SELECT COUNT(*) FROM #NoteAfter WHERE ExistingNoteId IS NOT NULL)
            AS NotesAlreadyOrNowImported,
       (SELECT COUNT(*) FROM #NoteAfter WHERE ExistingNoteId IS NOT NULL
          AND IsDeleted = 1) AS NotesExistingButSoftDeleted,
       (SELECT COUNT(*) FROM #NoteAfter WHERE IsPending = 1)
            AS NotesRemainingToInsert,
       (SELECT COUNT(*) FROM #NoteAfter WHERE IsBlocked = 1)
            AS NotesNeedingCustomerOrLengthReview,
       (SELECT COUNT(DISTINCT qc.DealId) FROM dbo.QualityControls AS qc
        WHERE qc.IsDeleted = 0 AND
         (qc.[Status] = 2 OR EXISTS
          (SELECT 1 FROM dbo.QualityControlAnswers AS ans
           WHERE ans.QualityControlId = qc.Id AND ans.IsDeleted = 0 AND ans.Answer = 1)))
            AS DiscrepantDealsUntouched,
       (SELECT COUNT(*) FROM dbo.QualityControls AS qc
        WHERE qc.IsDeleted = 0 AND qc.[Status] IN (4,5))
            AS NoAnswerOrInvalidNumberLegacyRowsUntouched;

-- Breakdown by deal; helps verify how many cases are still open.
SELECT d.DealNumber AS [شماره پرونده],
       d.EligibleItems AS [تعداد اقلام واجد شرایط],
       d.EligibleItems - d.PendingItems - d.ConflictingItems AS [اقلام بدون مغایرت ثبت شده],
       d.PendingItems AS [اقلام باقی مانده],
       d.ConflictingItems AS [اقلام نیازمند بررسی],
       d.PendingNotes AS [یادداشت باقی مانده],
       d.BlockedNotes AS [یادداشت با مشتری نامشخص],
       CASE
           WHEN d.EligibleItems = 0 THEN N'بدون اقلام فاکتور واجد شرایط'
           WHEN d.ConflictingItems > 0 OR d.BlockedNotes > 0 THEN N'نیازمند بررسی دستی'
           WHEN d.PendingItems > 0 OR d.PendingNotes > 0 THEN N'باقی مانده'
           ELSE N'تکمیل شده'
       END AS [وضعیت بک فیل]
FROM #DealProgress AS d
ORDER BY d.DealNumber DESC;

-- Explicitly list historical notes blocked due to customer ambiguity/length.
SELECT n.DealNumber AS [شماره پرونده], n.LegacyQualityControlId,
       LEFT(n.LegacyDescription, 180) AS [بخشی از توضیحات],
       CASE
           WHEN n.ProposedCustomerId IS NULL THEN N'مشتری مشخص نیست یا چند مالک دارد'
           WHEN LEN(n.LegacyDescription) > 4000 THEN N'طول متن بیش از ۴۰۰۰ کاراکتر'
           ELSE N'مشتری حذف یا پیدا نشده است'
       END AS [علت عدم انتقال]
FROM #NoteAfter AS n
WHERE n.IsBlocked = 1
ORDER BY n.DealNumber DESC;

DROP TABLE IF EXISTS #DealProgress;
DROP TABLE IF EXISTS #NoteAfter;
DROP TABLE IF EXISTS #ItemAfter;
DROP TABLE IF EXISTS #InsertedNotes;
DROP TABLE IF EXISTS #InsertedItems;
DROP TABLE IF EXISTS #LegacyNotes;
DROP TABLE IF EXISTS #EligibleItems;
DROP TABLE IF EXISTS #SafeLegacy;
