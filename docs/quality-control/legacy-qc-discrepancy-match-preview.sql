/*
  QC legacy discrepancy -> invoice-item suggestion preview (READ ONLY).
  This is text/phrase matching, NOT semantic verification or approval.
  Every candidate must be confirmed by a human with FactorItemId.
  No invoice-item review or original QC row is created/updated/deleted.
  Run in the CRM database after the QC schema migrations.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.QualityControls', N'U') IS NULL
   OR OBJECT_ID(N'dbo.QualityControlAnswers', N'U') IS NULL
   OR OBJECT_ID(N'dbo.QualityControlItemReviews', N'U') IS NULL
   OR OBJECT_ID(N'dbo.FactorItems', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Factors', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Deals', N'U') IS NULL
    THROW 51011, 'Required QC tables are missing in the selected CRM database.', 1;

DECLARE @MinPhraseLength int = 5;  -- shorter/generic product labels: manual inspection only

DROP TABLE IF EXISTS #LegacyDiscrepancies;
DROP TABLE IF EXISTS #DiscrepancyCandidates;

SELECT qc.Id AS LegacyQualityControlId,
       qc.DealId,
       d.SerialNumber AS DealNumber,
       d.Title AS DealTitle,
       d.CustomerName,
       d.FinancialStatus,
       qc.[Status] AS LegacyStatus,
       qc.[Description] AS LegacyDescription,
       qc.CreateDate AS ReviewedOn,
       (SELECT COUNT(*)
        FROM dbo.QualityControlAnswers AS a
        WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0 AND a.Answer = 1) AS PositiveAnswerCount
INTO #LegacyDiscrepancies
FROM dbo.QualityControls AS qc
INNER JOIN dbo.Deals AS d ON d.Id = qc.DealId
WHERE qc.IsDeleted = 0 AND d.IsDeleted = 0
  AND (qc.[Status] = 2
       OR EXISTS (SELECT 1 FROM dbo.QualityControlAnswers AS a
                  WHERE a.QualityControlId = qc.Id
                    AND a.IsDeleted = 0 AND a.Answer = 1));

SELECT legacy.LegacyQualityControlId, legacy.DealId, legacy.DealNumber,
       legacy.DealTitle, legacy.CustomerName, legacy.LegacyStatus,
       legacy.PositiveAnswerCount, legacy.LegacyDescription,
       f.Id AS FactorId, f.SerialNumber AS FactorNumber,
       fi.Id AS FactorItemId, fi.[Row] AS FactorItemRow,
       fi.ProductSmallName, fi.Product,
       fi.WeightCount, fi.UnitTxt,
       review.Id AS ExistingItemReviewId,
       review.IsDeleted AS ExistingItemReviewIsDeleted,
       CASE WHEN LEN(txt.SmallName) >= @MinPhraseLength
                      AND CHARINDEX(N' ' + txt.SmallName + N' ', N' ' + txt.Comment + N' ') > 0
                 THEN N'نام کوتاه محصول'
            ELSE N'نام کامل محصول'
       END AS MatchedOn
INTO #DiscrepancyCandidates
FROM #LegacyDiscrepancies AS legacy
INNER JOIN dbo.Factors AS f ON f.DealId = legacy.DealId
    AND f.IsDeleted = 0 AND f.[status] = 2 AND f.Returned = 0
INNER JOIN dbo.FactorItems AS fi ON fi.factorId = f.Id AND fi.IsDeleted = 0
LEFT JOIN dbo.QualityControlItemReviews AS review ON review.FactorItemId = fi.Id
CROSS APPLY
(
    SELECT LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(LOWER(COALESCE(legacy.LegacyDescription, N'')), N'ي', N'ی'), N'ى', N'ی'), N'ك', N'ک'), NCHAR(8204), N' '), NCHAR(8205), N' '), NCHAR(8206), N' '), CHAR(9), N' '), CHAR(10), N' '), CHAR(13), N' '), N'،', N' '), N'؛', N' '), N'.', N' '), N'/', N' '), N'-', N' '), N'(', N' '), N')', N' '), N':', N' '), N'«', N' '), N'»', N' '), N'  ', N' '), N'  ', N' '), N'  ', N' '))) AS Comment,
           LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(LOWER(COALESCE(fi.ProductSmallName, N'')), N'ي', N'ی'), N'ى', N'ی'), N'ك', N'ک'), NCHAR(8204), N' '), NCHAR(8205), N' '), NCHAR(8206), N' '), CHAR(9), N' '), CHAR(10), N' '), CHAR(13), N' '), N'،', N' '), N'؛', N' '), N'.', N' '), N'/', N' '), N'-', N' '), N'(', N' '), N')', N' '), N':', N' '), N'«', N' '), N'»', N' '), N'  ', N' '), N'  ', N' '), N'  ', N' '))) AS SmallName,
           LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(LOWER(COALESCE(fi.Product, N'')), N'ي', N'ی'), N'ى', N'ی'), N'ك', N'ک'), NCHAR(8204), N' '), NCHAR(8205), N' '), NCHAR(8206), N' '), CHAR(9), N' '), CHAR(10), N' '), CHAR(13), N' '), N'،', N' '), N'؛', N' '), N'.', N' '), N'/', N' '), N'-', N' '), N'(', N' '), N')', N' '), N':', N' '), N'«', N' '), N'»', N' '), N'  ', N' '), N'  ', N' '), N'  ', N' '))) AS FullName
) AS txt
WHERE (
    LEN(txt.SmallName) >= @MinPhraseLength
    AND CHARINDEX(N' ' + txt.SmallName + N' ', N' ' + txt.Comment + N' ') > 0
) OR (
    LEN(txt.FullName) >= @MinPhraseLength
    AND CHARINDEX(N' ' + txt.FullName + N' ', N' ' + txt.Comment + N' ') > 0
);

-- 1) One row per legacy discrepant deal, including zero-match cases.
SELECT legacy.DealNumber AS [شماره پرونده],
       legacy.DealTitle AS [عنوان پرونده],
       legacy.CustomerName AS [مشتری],
       legacy.LegacyStatus AS [وضعیت قدیمی],
       legacy.PositiveAnswerCount AS [پاسخ های مغایرت دار],
       legacy.LegacyDescription AS [توضیحات قدیمی],
       legacy.LegacyQualityControlId,
       COALESCE(s.TotalEligibleItems, 0) AS EligibleInvoiceItems,
       COALESCE(m.MatchedItems, 0) AS SuggestedItems,
       CASE
         WHEN NULLIF(LTRIM(RTRIM(legacy.LegacyDescription)), N'') IS NULL
            THEN N'توضیحی برای شناسایی قلم ندارد؛ بررسی دستی'
         WHEN COALESCE(m.MatchedItems, 0) = 0
            THEN N'نام کالا در توضیحات پیدا نشد؛ بررسی دستی'
         WHEN COALESCE(m.MatchedItems, 0) = 1
            THEN N'یک قلم پیشنهاد شده؛ نیازمند تایید دستی شناسه قلم'
         ELSE N'چند قلم پیشنهاد شده؛ انتخاب دستی قلم یا اقلام صحیح'
       END AS [نتیجه پیشنهادی]
FROM #LegacyDiscrepancies AS legacy
OUTER APPLY (
    SELECT COUNT(*) AS TotalEligibleItems
    FROM dbo.Factors AS f
    INNER JOIN dbo.FactorItems AS fi ON fi.factorId = f.Id AND fi.IsDeleted = 0
    WHERE f.DealId = legacy.DealId AND f.IsDeleted = 0
      AND f.[status] = 2 AND f.Returned = 0
) AS s
OUTER APPLY (
    SELECT COUNT(*) AS MatchedItems
    FROM #DiscrepancyCandidates AS c
    WHERE c.LegacyQualityControlId = legacy.LegacyQualityControlId
) AS m
ORDER BY legacy.DealNumber DESC;

-- 2) Concrete candidate invoice lines. CandidateCount > 1 is ambiguous.
-- Copy GUIDs only after checking the original comment and invoice item.
SELECT c.DealNumber AS [شماره پرونده],
       c.FactorNumber AS [شماره فاکتور],
       c.FactorItemRow AS [ردیف قلم],
       c.ProductSmallName AS [عنوان کوتاه کالا],
       c.Product AS [شرح کامل کالا],
       c.WeightCount AS [مقدار],
       c.UnitTxt AS [واحد],
       c.MatchedOn AS [دلیل پیشنهاد],
       c.LegacyDescription AS [توضیح کنترل کیفیت قدیمی],
       COUNT(*) OVER (PARTITION BY c.LegacyQualityControlId) AS [تعداد اقلام پیشنهادی],
       CASE WHEN c.ExistingItemReviewId IS NOT NULL
            THEN N'قبلا بررسی شده؛ انتقال ممنوع'
            WHEN COUNT(*) OVER (PARTITION BY c.LegacyQualityControlId) > 1
            THEN N'چند گزینه؛ انتخاب دستی'
            ELSE N'یک پیشنهاد؛ بررسی و تایید دستی'
       END AS [وضعیت تطبیق],
       c.LegacyQualityControlId, c.DealId, c.FactorId, c.FactorItemId,
       c.ExistingItemReviewId, c.ExistingItemReviewIsDeleted
FROM #DiscrepancyCandidates AS c
ORDER BY c.DealNumber DESC, c.FactorNumber DESC, c.FactorItemRow;

-- 3) Totals. Number of legacy discrepancy records is not the number of invoice items.
SELECT (SELECT COUNT(*) FROM #LegacyDiscrepancies) AS DiscrepantLegacyDeals,
       (SELECT COUNT(DISTINCT LegacyQualityControlId) FROM #DiscrepancyCandidates)
          AS DealsWithTextCandidates,
       (SELECT COUNT(*) FROM #LegacyDiscrepancies
        WHERE LegacyQualityControlId NOT IN
            (SELECT LegacyQualityControlId FROM #DiscrepancyCandidates)) AS DealsWithoutTextCandidates,
       (SELECT COUNT(*) FROM #DiscrepancyCandidates) AS SuggestedInvoiceItems,
       (SELECT COUNT(*) FROM #DiscrepancyCandidates WHERE ExistingItemReviewId IS NOT NULL)
          AS AlreadyReviewedSuggestions;

DROP TABLE IF EXISTS #DiscrepancyCandidates;
DROP TABLE IF EXISTS #LegacyDiscrepancies;
