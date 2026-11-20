/*
  Legacy QC discrepancy manual review worklist (READ ONLY)
  ---------------------------------------------------------
  One SSMS results grid suitable for Results > Save Results As... (CSV).
  Every old discrepancy is shown, including deals without eligible invoice lines.
  All eligible invoice lines are shown; no automatic item selection is made.

  Human review workflow:
    Decision: 'دارای مغایرت' / 'نامرتبط' / 'نامشخص' / 'مغایرت سطح سفارش'
    For a discrepancy of a specific item, verify FactorItemId and preserve
    its exact paired LegacyQualityControlId and a written approval reason.
    Review duplicates and mixed (quality / amount / delivery) descriptions.
  NEVER paste unreviewed matches into the approved-apply script.
  NO persistent database rows are inserted, updated or deleted.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.QualityControls', N'U') IS NULL
 OR OBJECT_ID(N'dbo.QualityControlAnswers', N'U') IS NULL
 OR OBJECT_ID(N'dbo.Deals', N'U') IS NULL
 OR OBJECT_ID(N'dbo.OrderHeads', N'U') IS NULL
 OR OBJECT_ID(N'dbo.Factors', N'U') IS NULL
 OR OBJECT_ID(N'dbo.FactorItems', N'U') IS NULL
 OR OBJECT_ID(N'dbo.QualityControlItemReviews', N'U') IS NULL
    THROW 51031, 'Required QC schema is missing; check the selected CRM database.', 1;

;WITH Legacy AS
(
    SELECT qc.Id AS LegacyQualityControlId,
           qc.DealId,
           d.SerialNumber AS DealNumber,
           d.Title AS DealTitle,
           d.CustomerName,
           d.FinancialStatus,
           qc.[Status] AS LegacyStatus,
           qc.[Description] AS LegacyDescription,
           qc.CreateDate AS LegacyReviewedOn,
           qc.UserCreator AS LegacyReviewedBy,
           answers.PositiveAnswerCount,
           answers.PositiveQuestionTitles
    FROM dbo.QualityControls AS qc
    INNER JOIN dbo.Deals AS d ON d.Id = qc.DealId AND d.IsDeleted = 0
    OUTER APPLY
    (
        SELECT COUNT(*) AS PositiveAnswerCount,
               STRING_AGG(
                   CONVERT(nvarchar(max),
                       COALESCE(NULLIF(a.QuestionTitleSnapshot,N''),N'سؤال نامشخص')),
                   N' | ') AS PositiveQuestionTitles
        FROM dbo.QualityControlAnswers AS a
        WHERE a.QualityControlId = qc.Id
          AND a.IsDeleted = 0 AND a.Answer = 1
    ) AS answers
    WHERE qc.IsDeleted = 0
      AND (qc.[Status] = 2 OR answers.PositiveAnswerCount > 0)
),
Worklist AS
(
    SELECT legacy.*,
           orders.OrderNumbers,
           line.FactorId,
           line.FactorNumber,
           line.FactorItemId,
           line.FactorItemRow,
           line.ProductSmallName,
           line.Product,
           line.WeightCount,
           line.UnitTxt,
           previous.Id AS ExistingItemReviewId,
           previous.[Status] AS ExistingItemReviewStatus,
           previous.IsDeleted AS ExistingItemReviewIsDeleted
    FROM Legacy AS legacy
    OUTER APPLY
    (
        SELECT STRING_AGG(CONVERT(nvarchar(max), oh.OrderNumber), N'، ')
                   AS OrderNumbers
        FROM dbo.OrderHeads AS oh
        WHERE oh.DealId = legacy.DealId AND oh.IsDeleted = 0
    ) AS orders
    OUTER APPLY
    (
        SELECT f.Id AS FactorId, f.SerialNumber AS FactorNumber,
               fi.Id AS FactorItemId, fi.[Row] AS FactorItemRow,
               fi.ProductSmallName, fi.Product, fi.WeightCount, fi.UnitTxt
        FROM dbo.Factors AS f
        INNER JOIN dbo.FactorItems AS fi
            ON fi.factorId = f.Id AND fi.IsDeleted = 0
        WHERE f.DealId = legacy.DealId
          AND legacy.FinancialStatus = 1
          AND f.IsDeleted = 0 AND f.[status] = 2 AND f.Returned = 0
    ) AS line
    LEFT JOIN dbo.QualityControlItemReviews AS previous
        ON previous.FactorItemId = line.FactorItemId
)
SELECT w.DealNumber AS [شماره پرونده],
       w.OrderNumbers AS [شماره حواله های پرونده],
       w.CustomerName AS [مشتری],
       w.DealTitle AS [عنوان پرونده],
       w.LegacyStatus AS [وضعیت قدیمی],
       w.PositiveAnswerCount AS [تعداد سوالات دارای مغایرت],
       w.PositiveQuestionTitles AS [سوالات دارای مغایرت],
       w.LegacyDescription AS [متن کامل توضیحات قدیمی],
       CASE
           WHEN (w.PositiveQuestionTitles LIKE N'%زمان تحویل%'
                OR w.LegacyDescription LIKE N'%تاخیر%'
                OR w.LegacyDescription LIKE N'%تأخیر%'
                OR w.LegacyDescription LIKE N'%دیرکرد%')
             THEN N'دارد'
           ELSE N'ندارد'
       END AS [نشانه مربوط به زمان تحویل],
       CASE
           WHEN (w.PositiveQuestionTitles LIKE N'%مقدار%'
                OR w.PositiveQuestionTitles LIKE N'%وزن%'
                OR w.LegacyDescription LIKE N'%کسری%'
                OR w.LegacyDescription LIKE N'%متراژ%')
             THEN N'دارد'
           ELSE N'ندارد'
       END AS [نشانه مربوط به مقدار یا وزن],
       CASE
           WHEN (w.PositiveQuestionTitles LIKE N'%کیفیت%'
                OR w.LegacyDescription LIKE N'%کیفیت%'
                OR w.LegacyDescription LIKE N'%دوخت%'
                OR w.LegacyDescription LIKE N'%چاپ%')
             THEN N'دارد'
           ELSE N'ندارد'
       END AS [نشانه مربوط به کیفیت],
       w.FactorNumber AS [شماره فاکتور],
       w.FactorItemRow AS [ردیف قلم],
       w.ProductSmallName AS [عنوان کوتاه کالا],
       w.Product AS [شرح کامل کالا],
       w.WeightCount AS [مقدار],
       w.UnitTxt AS [واحد],
       COUNT(w.FactorItemId) OVER
         (PARTITION BY w.LegacyQualityControlId) AS [تعداد اقلام واجد شرایط پرونده],
       CASE
           WHEN w.FactorItemId IS NULL
             THEN N'قلم فاکتور واجد شرایط ندارد؛ بررسی دستی'
           WHEN w.ExistingItemReviewId IS NOT NULL
             THEN N'دارای کنترل کیفیت جدید؛ ثبت دوباره ممنوع'
           ELSE N'فقط برای بررسی؛ نیازمند تایید دستی'
       END AS [وضعیت قلم برای بک فیل],
       CONVERT(nvarchar(30),N'') AS [تصمیم کارشناس],
       CONVERT(nvarchar(400),N'') AS [دلیل انتخاب قلم],
       w.LegacyQualityControlId AS [شناسه کنترل کیفیت قدیمی],
       w.FactorItemId AS [شناسه قلم فاکتور],
       w.FactorId AS [شناسه فاکتور],
       w.DealId AS [شناسه پرونده],
       w.ExistingItemReviewId AS [شناسه کنترل کیفیت جدید],
       w.ExistingItemReviewStatus AS [وضعیت کنترل کیفیت جدید],
       w.ExistingItemReviewIsDeleted AS [کنترل کیفیت جدید حذف نرم شده],
       w.LegacyReviewedOn AS [تاریخ کنترل کیفیت قدیمی],
       w.LegacyReviewedBy AS [ثبت کننده قدیمی]
FROM Worklist AS w
ORDER BY w.DealNumber DESC, w.FactorNumber DESC, w.FactorItemRow,
         w.LegacyQualityControlId;
