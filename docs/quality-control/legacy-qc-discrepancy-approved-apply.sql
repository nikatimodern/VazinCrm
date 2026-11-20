/*
  QC discrepancy backfill: explicit, human-approved mappings ONLY.
  1) Run legacy-qc-discrepancy-match-preview.sql first.
  2) Verify EVERY proposed FactorItemId against source notes and invoice.
  3) Enter exact GUID pairs below with approval reason.
  4) Preview with @Apply=0. Back up the DB, then @BackupConfirmed=1, @Apply=1.
  Never infer that other lines of a discrepant deal were discrepancy-free.
  No original rows are deleted and no existing item review is overwritten.
  No legacy deal-level question answers are copied to a specific invoice line.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply bit = 0;
DECLARE @BackupConfirmed bit = 0;

DECLARE @Approved TABLE
(
    LegacyQualityControlId uniqueidentifier NOT NULL,
    FactorItemId uniqueidentifier NOT NULL PRIMARY KEY,
    ApprovalReason nvarchar(400) NOT NULL
);

-- Example only: remove comment after replacing ALL identifiers with verified GUIDs.
-- INSERT INTO @Approved (LegacyQualityControlId, FactorItemId, ApprovalReason)
-- VALUES ('00000000-0000-0000-0000-000000000000',
--         '00000000-0000-0000-0000-000000000000', N'نام کالا و شماره فاکتور با توضیح قبلی تطبیق داده شد');

IF OBJECT_ID(N'dbo.QualityControls', N'U') IS NULL
   OR OBJECT_ID(N'dbo.QualityControlAnswers', N'U') IS NULL
   OR OBJECT_ID(N'dbo.QualityControlItemReviews', N'U') IS NULL
   OR OBJECT_ID(N'dbo.FactorItems', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Factors', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Deals', N'U') IS NULL
    THROW 51021, 'Required QC tables are missing in the selected CRM database.', 1;

-- Preview approved pairs and validate linked deal, invoice and line.
DROP TABLE IF EXISTS #ApprovedValidation;
SELECT a.LegacyQualityControlId, a.FactorItemId, a.ApprovalReason,
       d.SerialNumber AS DealNumber, f.SerialNumber AS FactorNumber,
       fi.[Row] AS FactorItemRow, fi.ProductSmallName, fi.Product,
       qc.[Status] AS LegacyStatus, qc.[Description] AS LegacyDescription,
       ir.Id AS ExistingReviewId, ir.IsDeleted AS ExistingReviewIsDeleted,
       CASE
         WHEN qc.Id IS NULL OR d.Id IS NULL OR f.Id IS NULL OR fi.Id IS NULL
             THEN N'نامعتبر؛ رکورد یا ارتباط فاکتور پیدا نشد'
         WHEN qc.IsDeleted = 1 OR d.IsDeleted = 1 OR d.FinancialStatus <> 1
             OR f.IsDeleted = 1 OR f.[status] <> 2 OR f.Returned = 1 OR fi.IsDeleted = 1
             THEN N'نامعتبر؛ رکورد از چرخه کنترل کیفیت خارج شده است'
         WHEN (qc.[Status] IS NULL OR qc.[Status] <> 2) AND NOT EXISTS
             (SELECT 1 FROM dbo.QualityControlAnswers AS ans
              WHERE ans.QualityControlId = qc.Id AND ans.IsDeleted = 0 AND ans.Answer = 1)
             THEN N'نامعتبر؛ کنترل کیفیت قدیمی مغایرت ثبت شده ندارد'
         WHEN NULLIF(LTRIM(RTRIM(qc.[Description])), N'') IS NULL
             THEN N'نامعتبر؛ توضیح قدیمی برای تایید تطبیق وجود ندارد'
         WHEN LEN(qc.[Description]) > 4000
             THEN N'نامعتبر؛ شرح بیش از ۴۰۰۰ کاراکتر است'
         WHEN NULLIF(LTRIM(RTRIM(a.ApprovalReason)), N'') IS NULL
             THEN N'نامعتبر؛ دلیل تایید وارد نشده است'
         WHEN ir.Id IS NOT NULL
             THEN N'ممنوع؛ قلم قبلاً نتیجه کنترل کیفیت دارد (حتی حذف نرم)'
         ELSE N'آماده ثبت پس از تایید و تهیه بکاپ'
       END AS ApprovalStatus
INTO #ApprovedValidation
FROM @Approved AS a
LEFT JOIN dbo.QualityControls AS qc ON qc.Id = a.LegacyQualityControlId
LEFT JOIN dbo.Deals AS d ON d.Id = qc.DealId
LEFT JOIN dbo.Factors AS f ON f.DealId = d.Id
    AND EXISTS (SELECT 1 FROM dbo.FactorItems AS fi2
                WHERE fi2.Id = a.FactorItemId AND fi2.factorId = f.Id)
LEFT JOIN dbo.FactorItems AS fi ON fi.Id = a.FactorItemId AND fi.factorId = f.Id
LEFT JOIN dbo.QualityControlItemReviews AS ir ON ir.FactorItemId = a.FactorItemId;

SELECT * FROM #ApprovedValidation ORDER BY DealNumber DESC, FactorNumber DESC;
SELECT COUNT(*) AS TotalApprovedPairs,
       COALESCE(SUM(CASE WHEN ApprovalStatus = N'آماده ثبت پس از تایید و تهیه بکاپ' THEN 1 ELSE 0 END),0)
           AS ReadyPairs,
       COALESCE(SUM(CASE WHEN ApprovalStatus <> N'آماده ثبت پس از تایید و تهیه بکاپ' THEN 1 ELSE 0 END),0)
           AS RejectedPairs
FROM #ApprovedValidation;

IF @Apply = 1
BEGIN
    IF @BackupConfirmed <> 1
        THROW 51022, 'Confirm a full DB backup by setting @BackupConfirmed=1.', 1;
    IF NOT EXISTS (SELECT 1 FROM @Approved)
        THROW 51023, 'No manually approved invoice-item mappings were provided.', 1;
    IF EXISTS (SELECT 1 FROM #ApprovedValidation
               WHERE ApprovalStatus <> N'آماده ثبت پس از تایید و تهیه بکاپ')
        THROW 51024, 'At least one approved mapping is invalid. Review the preview.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;
        -- Re-check target inside the transaction to guard against concurrent QC writes.
        IF EXISTS
        (
            SELECT 1
            FROM @Approved AS a
            INNER JOIN dbo.QualityControlItemReviews AS r WITH (UPDLOCK, HOLDLOCK)
                ON r.FactorItemId = a.FactorItemId
        )
            THROW 51025, 'An invoice-item review was registered concurrently. No rows inserted.', 1;

        INSERT INTO dbo.QualityControlItemReviews
        (
            Id, FactorItemId, [Status], [Description],
            CreateDate, ModifyDate, CreateDateTxt, ModifyDateTxt,
            IsDeleted, UserCreatorID, UserCreator, UserModifyID, UserModify, CommentText
        )
        SELECT NEWID(), a.FactorItemId, 2, qc.[Description],
               qc.CreateDate, COALESCE(qc.ModifyDate, qc.CreateDate),
               qc.CreateDateTxt, COALESCE(qc.ModifyDateTxt, qc.CreateDateTxt),
               0, qc.UserCreatorID, qc.UserCreator,
               qc.UserModifyID, qc.UserModify,
               CONCAT(N'بک‌فیل مغایرت قدیمی با تایید دستی؛ شناسه کنترل کیفیت: ',
                      CONVERT(nvarchar(36), qc.Id), N'؛ دلیل تایید: ', a.ApprovalReason)
        FROM @Approved AS a
        INNER JOIN dbo.QualityControls AS qc ON qc.Id = a.LegacyQualityControlId
        INNER JOIN dbo.Deals AS d ON d.Id = qc.DealId
        INNER JOIN dbo.FactorItems AS fi ON fi.Id = a.FactorItemId
        INNER JOIN dbo.Factors AS f ON f.Id = fi.factorId AND f.DealId = d.Id
        WHERE qc.IsDeleted = 0 AND d.IsDeleted = 0 AND d.FinancialStatus = 1
          AND f.IsDeleted = 0 AND f.[status] = 2 AND f.Returned = 0
          AND fi.IsDeleted = 0
          AND (qc.[Status] = 2 OR EXISTS
              (SELECT 1 FROM dbo.QualityControlAnswers AS ans
               WHERE ans.QualityControlId = qc.Id AND ans.IsDeleted = 0 AND ans.Answer = 1))
          AND NOT EXISTS
              (SELECT 1 FROM dbo.QualityControlItemReviews AS r WITH (UPDLOCK, HOLDLOCK)
               WHERE r.FactorItemId = a.FactorItemId);

        IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @Approved)
            THROW 51026, 'Source mapping changed during apply. Transaction rolled back.', 1;

        COMMIT TRANSACTION;
        SELECT N'تطبیق های تاییدشده با وضعیت دارای مغایرت ثبت شدند.' AS Result,
               (SELECT COUNT(*) FROM @Approved) AS InsertedItemReviews;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
ELSE
    SELECT N'فقط پیش نمایش؛ داده ای تغییر نکرد. @Apply=0.' AS Result;

DROP TABLE IF EXISTS #ApprovedValidation;
