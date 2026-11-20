/*
  Legacy QC discrepancy -> SPECIFIC invoice-item matching PREVIEW (READ ONLY) V2
  SQL Server 2019+ and database compatibility level 130+ (STRING_SPLIT).
  
  Main safety improvement:
  - "نایلون", "سلفون", "سفره", "پاکت" and other generic terms NEVER
    generate a candidate on their own.
  - Evidence comes from matched distinctive size/number tokens, or
    uncommon descriptive tokens, and is ranked across items within the deal.
  - Context of discrepancy (quality / quantity / delivery) is shown separately.
    Delivery delay alone does NOT identify a specific invoice line.
  - Quantity and invoice serial are only supporting clues, not automatic approval.
  - 0/1/multiple candidates are always MANUALLY VERIFIED.
  - Does not update ANY persistent tables or approve any mapping.
  Use docs/quality-control/legacy-qc-discrepancy-approved-apply.sql
  ONLY after checking exact FactorItemId + LegacyQualityControlId.
*/
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.QualityControls', N'U') IS NULL
   OR OBJECT_ID(N'dbo.QualityControlAnswers', N'U') IS NULL
   OR OBJECT_ID(N'dbo.QualityControlItemReviews', N'U') IS NULL
   OR OBJECT_ID(N'dbo.FactorItems', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Factors', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Deals', N'U') IS NULL
    THROW 51011, 'Required QC tables are missing in the selected CRM database.', 1;

IF (SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()) < 130
    THROW 51012, 'This preview requires database compatibility level 130 or higher for STRING_SPLIT.', 1;

DROP TABLE IF EXISTS #LegacyDiscrepancies;
DROP TABLE IF EXISTS #InvoiceLines;
DROP TABLE IF EXISTS #ProductTokens;
DROP TABLE IF EXISTS #CommentTokens;
DROP TABLE IF EXISTS #TokenUsage;
DROP TABLE IF EXISTS #MatchedTokens;
DROP TABLE IF EXISTS #RankedLines;

-- The discrepancy belongs to an old DEAL, not automatically to every invoice line.
SELECT qc.Id AS LegacyQualityControlId,
       qc.DealId,
       d.SerialNumber AS DealNumber, d.Title AS DealTitle,
       d.CustomerName, d.FinancialStatus, qc.[Status] AS LegacyStatus,
       qc.[Description] AS LegacyDescription, qc.CreateDate AS ReviewedOn,
       LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(TRANSLATE(LOWER(CONVERT(nvarchar(max), COALESCE(qc.[Description], N''))), N'۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩', N'01234567890123456789'), N'ي', N'ی'), N'ى', N'ی'), N'ك', N'ک'), NCHAR(8204), N' '), NCHAR(8205), N' '), NCHAR(8206), N' '), NCHAR(160), N' '), NCHAR(9), N' '), NCHAR(10), N' '), NCHAR(13), N' '), N'×', N' '), N'*', N' '), N'x', N' '), N'/', N' '), N'\', N' '), N'_', N' '), N'-', N' '), N',', N' '), N'،', N' '), N'؛', N' '), N';', N' '), N':', N' '), N'.', N' '), N'(', N' '), N')', N' '), N'[', N' '), N']', N' '), N'«', N' '), N'»', N' '), N'  ', N' '), N'  ', N' '), N'  ', N' '))) AS NormalizedComment,
       (SELECT COUNT(*)
        FROM dbo.QualityControlAnswers AS a
        WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0 AND a.Answer = 1) AS PositiveAnswerCount,
       (SELECT STRING_AGG(CONVERT(nvarchar(max), COALESCE(NULLIF(a.QuestionTitleSnapshot,N''), N'سؤال نامشخص')), N'، ')
        FROM dbo.QualityControlAnswers AS a
        WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0 AND a.Answer = 1) AS PositiveQuestionTitles
INTO #LegacyDiscrepancies
FROM dbo.QualityControls AS qc
INNER JOIN dbo.Deals AS d ON d.Id = qc.DealId
WHERE qc.IsDeleted = 0 AND d.IsDeleted = 0
  AND (qc.[Status] = 2 OR EXISTS (
       SELECT 1 FROM dbo.QualityControlAnswers AS a
       WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0 AND a.Answer = 1));

-- All eligible invoice lines of discrepant deals, including already-reviewed ones.
-- Never skip existing results silently: expose them and mark them blocked.
SELECT l.LegacyQualityControlId, l.DealId, l.DealNumber, l.DealTitle,
       l.CustomerName, l.LegacyStatus, l.LegacyDescription,
       l.NormalizedComment, l.PositiveAnswerCount, l.PositiveQuestionTitles,
       f.Id AS FactorId, f.SerialNumber AS FactorNumber,
       fi.Id AS FactorItemId, fi.[Row] AS FactorItemRow,
       fi.ProductSmallName, fi.Product, fi.WeightCount, fi.UnitTxt,
       review.Id AS ExistingItemReviewId, review.IsDeleted AS ExistingItemReviewIsDeleted,
       LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(TRANSLATE(LOWER(CONVERT(nvarchar(max), COALESCE(CONCAT(COALESCE(fi.ProductSmallName,N''),N' ',COALESCE(fi.Product,N'')), N''))), N'۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩', N'01234567890123456789'), N'ي', N'ی'), N'ى', N'ی'), N'ك', N'ک'), NCHAR(8204), N' '), NCHAR(8205), N' '), NCHAR(8206), N' '), NCHAR(160), N' '), NCHAR(9), N' '), NCHAR(10), N' '), NCHAR(13), N' '), N'×', N' '), N'*', N' '), N'x', N' '), N'/', N' '), N'\', N' '), N'_', N' '), N'-', N' '), N',', N' '), N'،', N' '), N'؛', N' '), N';', N' '), N':', N' '), N'.', N' '), N'(', N' '), N')', N' '), N'[', N' '), N']', N' '), N'«', N' '), N'»', N' '), N'  ', N' '), N'  ', N' '), N'  ', N' '))) AS NormalizedProduct,
       LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(TRANSLATE(LOWER(CONVERT(nvarchar(max), COALESCE(fi.WeightCount, N''))), N'۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩', N'01234567890123456789'), N'ي', N'ی'), N'ى', N'ی'), N'ك', N'ک'), NCHAR(8204), N' '), NCHAR(8205), N' '), NCHAR(8206), N' '), NCHAR(160), N' '), NCHAR(9), N' '), NCHAR(10), N' '), NCHAR(13), N' '), N'×', N' '), N'*', N' '), N'x', N' '), N'/', N' '), N'\', N' '), N'_', N' '), N'-', N' '), N',', N' '), N'،', N' '), N'؛', N' '), N';', N' '), N':', N' '), N'.', N' '), N'(', N' '), N')', N' '), N'[', N' '), N']', N' '), N'«', N' '), N'»', N' '), N'  ', N' '), N'  ', N' '), N'  ', N' '))) AS NormalizedQuantity
INTO #InvoiceLines
FROM #LegacyDiscrepancies AS l
INNER JOIN dbo.Factors AS f ON f.DealId = l.DealId
    AND l.FinancialStatus = 1
    AND f.IsDeleted = 0 AND f.[status] = 2 AND f.Returned = 0
INNER JOIN dbo.FactorItems AS fi ON fi.factorId = f.Id AND fi.IsDeleted = 0
LEFT JOIN dbo.QualityControlItemReviews AS review ON review.FactorItemId = fi.Id;

-- Generic words are excluded from token evidence, not from the visible description.
-- A bare product type (e.g. "نایلون") is a WEAK contextual mention, not a match.
SELECT DISTINCT i.DealId, i.FactorItemId, tok.Token,
       CASE WHEN tok.Token NOT LIKE N'%[^0-9]%' AND LEN(tok.Token) BETWEEN 2 AND 4
                 AND NOT (TRY_CONVERT(int,tok.Token) BETWEEN 1300 AND 1500)
            THEN 1 ELSE 0 END AS IsNumeric
INTO #ProductTokens
FROM #InvoiceLines AS i
CROSS APPLY STRING_SPLIT(i.NormalizedProduct, N' ') AS sp
CROSS APPLY (VALUES (CONVERT(nvarchar(32),LTRIM(RTRIM(sp.value))))) AS tok(Token)
WHERE LEN(sp.value) BETWEEN 2 AND 32
  AND (
      -- Numbers: dimensions, thickness and measurements; exclude single digit and Persian calendar years.
      (tok.Token NOT LIKE N'%[^0-9]%' AND LEN(tok.Token) BETWEEN 2 AND 4
       AND NOT (TRY_CONVERT(int,tok.Token) BETWEEN 1300 AND 1500))
      OR
      -- Meaningful descriptive tokens; generic product nouns alone never match.
      (LEN(tok.Token) >= 3 AND tok.Token LIKE N'%[^0-9]%'
       AND tok.Token NOT IN (
           N'نایلون',
           N'نایلکس',
           N'سلفون',
           N'سفره',
           N'پاکت',
           N'پلاستیک',
           N'پلاستیکی',
           N'رول',
           N'کیسه',
           N'لفاف',
           N'چاپ',
           N'چاپی',
           N'عرض',
           N'طول',
           N'جنس',
           N'نوع',
           N'کیفیت',
           N'ضخامت',
           N'میکرون',
           N'گرم',
           N'کیلو',
           N'کیلوگرم',
           N'وزن',
           N'مقدار',
           N'متراژ',
           N'تعداد',
           N'کارتن',
           N'عدد',
           N'بسته',
           N'بسته‌بندی',
           N'رنگ',
           N'رنگی',
           N'سفید',
           N'سیاه',
           N'مشکی',
           N'شفاف',
           N'شیری',
           N'محصول',
           N'محصولات',
           N'سفارش',
           N'فاکتور',
           N'مشتری',
           N'فروش',
           N'خرید',
           N'بار',
           N'بارگیری',
           N'تحویل',
           N'ارسال',
           N'دریافت',
           N'مغایرت',
           N'کم',
           N'بیشتر',
           N'کمتر',
           N'ناقص',
           N'اضافه',
           N'کسری',
           N'تفاوت',
           N'دارد',
           N'ندارد',
           N'بود',
           N'بودن',
           N'شده',
           N'است',
           N'هست',
           N'میباشد',
           N'نمیباشد',
           N'برای',
           N'در',
           N'از',
           N'به',
           N'با',
           N'این',
           N'آن',
           N'را',
           N'که',
           N'یک',
           N'دو',
           N'سه',
           N'چهار',
           N'پنج',
           N'تای',
           N'تا',
           N'و',
           N'یا',
           N'روی',
           N'بر',
           N'مورد',
           N'موارد',
           N'طبق',
           N'طبق',
           N'دلیل',
           N'سالم',
           N'خراب',
           N'سایز',
           N'ابعاد',
           N'تولید',
           N'تامین',
           N'تأمین',
           N'زمان',
           N'تاخیر',
           N'تأخیر',
           N'دیر',
           N'دیرکرد',
           N'سفارشات'
       ))
  );

-- One copy of each normalized word/number in each old comment.
SELECT DISTINCT l.LegacyQualityControlId, tok.Token
INTO #CommentTokens
FROM #LegacyDiscrepancies AS l
CROSS APPLY STRING_SPLIT(l.NormalizedComment, N' ') AS sp
CROSS APPLY (VALUES(CONVERT(nvarchar(32),LTRIM(RTRIM(sp.value))))) AS tok(Token)
WHERE LEN(sp.value) BETWEEN 2 AND 32;

-- A word appearing on every line of the same deal is less distinctive.
SELECT p.DealId, p.Token, COUNT(DISTINCT p.FactorItemId) AS LinesUsingToken
INTO #TokenUsage
FROM #ProductTokens AS p
GROUP BY p.DealId,p.Token;

-- Exact-token matches only. Never use substring matching for generic words.
SELECT DISTINCT i.LegacyQualityControlId, i.FactorItemId, p.Token, p.IsNumeric,
       u.LinesUsingToken
INTO #MatchedTokens
FROM #InvoiceLines AS i
INNER JOIN #ProductTokens AS p
    ON p.DealId = i.DealId AND p.FactorItemId = i.FactorItemId
INNER JOIN #CommentTokens AS c
    ON c.LegacyQualityControlId = i.LegacyQualityControlId AND c.Token = p.Token
INNER JOIN #TokenUsage AS u ON u.DealId = p.DealId AND u.Token = p.Token;

-- Classify EVIDENCE, not certainty. Identical goods can have multiple equal-ranked lines.
SELECT base.*, 
       CASE
         WHEN base.NumericHits >= 2
             OR (base.NumericHits >= 1 AND base.UniqueTextHits >= 1)
             OR base.UniqueTextHits >= 2
             THEN 3
         WHEN base.UniqueTextHits >= 1
             OR (base.NumericHits >= 1 AND base.NonGenericTextHits >= 1)
             OR (base.NumericHits >= 1 AND base.FactorNumberMentioned = 1)
             THEN 2
         WHEN base.NumericHits >= 1 OR base.NonGenericTextHits >= 1
              OR base.FactorNumberMentioned = 1 OR base.QuantityMentioned = 1
             THEN 1
         ELSE 0
       END AS EvidenceTier
INTO #RankedLines
FROM
(
    SELECT i.*,
           COALESCE(e.NumericHits,0) AS NumericHits,
           COALESCE(e.UniqueTextHits,0) AS UniqueTextHits,
           COALESCE(e.NonGenericTextHits,0) AS NonGenericTextHits,
           e.MatchedTokens,
           CASE WHEN LEN(CONVERT(nvarchar(30),i.FactorNumber)) >= 5
                     AND CHARINDEX(N' ' + CONVERT(nvarchar(30),i.FactorNumber) + N' ',
                                   N' ' + i.NormalizedComment + N' ') > 0
                 THEN 1 ELSE 0 END AS FactorNumberMentioned,
           CASE WHEN LEN(i.NormalizedQuantity) >= 3
                     AND TRY_CONVERT(decimal(18,3),i.NormalizedQuantity) IS NOT NULL
                     AND CHARINDEX(N' ' + i.NormalizedQuantity + N' ',
                                   N' ' + i.NormalizedComment + N' ') > 0
                 THEN 1 ELSE 0 END AS QuantityMentioned,
           CASE 
             WHEN (i.NormalizedComment LIKE N'%تاخیر%' OR i.NormalizedComment LIKE N'%تأخیر%'
                       OR i.NormalizedComment LIKE N'%دیرکرد%' OR i.NormalizedComment LIKE N'%دیر به دست%')
               THEN N'احتمالاً زمان تحویل'
             WHEN (i.NormalizedComment LIKE N'%کسری%' OR i.NormalizedComment LIKE N'%وزن%'
                       OR i.NormalizedComment LIKE N'%مقدار%' OR i.NormalizedComment LIKE N'%تعداد%'
                       OR i.NormalizedComment LIKE N'%متراژ%')
               THEN N'احتمالاً مقدار / وزن'
             WHEN (i.NormalizedComment LIKE N'%کیفیت%' OR i.NormalizedComment LIKE N'%چاپ%'
                       OR i.NormalizedComment LIKE N'%ضخامت%' OR i.NormalizedComment LIKE N'%رنگ%'
                       OR i.NormalizedComment LIKE N'%پارگی%' OR i.NormalizedComment LIKE N'%جنس%')
               THEN N'احتمالاً کیفیت'
             ELSE N'نامشخص؛ سؤال‌های مثبت قدیمی را ببینید'
           END AS IssueTypeHint
    FROM #InvoiceLines AS i
    OUTER APPLY
    (
        SELECT SUM(CASE WHEN m.IsNumeric=1 THEN 1 ELSE 0 END) AS NumericHits,
               SUM(CASE WHEN m.IsNumeric=0 AND m.LinesUsingToken=1 THEN 1 ELSE 0 END) AS UniqueTextHits,
               SUM(CASE WHEN m.IsNumeric=0 THEN 1 ELSE 0 END) AS NonGenericTextHits,
               STRING_AGG(CONVERT(nvarchar(max),m.Token),N'، ') AS MatchedTokens
        FROM #MatchedTokens AS m
        WHERE m.LegacyQualityControlId = i.LegacyQualityControlId
          AND m.FactorItemId = i.FactorItemId
    ) AS e
) AS base;

-- Report 1: ALL old discrepancy records, including zero candidates.
SELECT l.DealNumber AS [شماره پرونده],
       l.DealTitle AS [عنوان پرونده], l.CustomerName AS [مشتری],
       l.LegacyStatus AS [وضعیت قدیمی],
       l.PositiveAnswerCount AS [تعداد پاسخ‌های مثبت],
       l.PositiveQuestionTitles AS [سؤال‌های دارای مغایرت],
       l.LegacyDescription AS [توضیح قدیمی], l.LegacyQualityControlId,
       COALESCE(m.AllInvoiceItems,0) AS EligibleInvoiceItems,
       COALESCE(m.SuggestedItems,0) AS SuggestedItems,
       COALESCE(m.WeakHints,0) AS WeakHints,
       CASE
          WHEN NULLIF(LTRIM(RTRIM(l.LegacyDescription)),N'') IS NULL THEN N'بدون توضیح؛ بررسی دستی'
          WHEN COALESCE(m.AllInvoiceItems,0)=0 THEN N'فاکتور واجد شرایط ندارد'
          WHEN COALESCE(m.SuggestedItems,0)=0 AND COALESCE(m.WeakHints,0)>0
             THEN N'فقط سرنخ ضعیف؛ هیچ قلم قابل پیشنهاد نیست'
          WHEN COALESCE(m.SuggestedItems,0)=0 THEN N'تطبیق اختصاصی پیدا نشد؛ بررسی دستی'
          WHEN COALESCE(m.SuggestedItems,0)=1 THEN N'یک پیشنهاد احتمالی؛ تایید شناسه قلم الزامی'
          ELSE N'چند پیشنهاد احتمالی؛ انتخاب دستی قلم صحیح'
       END AS [نتیجه]
FROM #LegacyDiscrepancies AS l
OUTER APPLY 
(
  SELECT COUNT(*) AS AllInvoiceItems,
         SUM(CASE WHEN r.EvidenceTier>=2 THEN 1 ELSE 0 END) AS SuggestedItems,
         SUM(CASE WHEN r.EvidenceTier=1 THEN 1 ELSE 0 END) AS WeakHints
  FROM #RankedLines AS r WHERE r.LegacyQualityControlId=l.LegacyQualityControlId
) AS m
ORDER BY l.DealNumber DESC;

-- Report 2: meaningful suggestions FIRST, weaker clues clearly labeled.
-- Even a unique, well-scored line is NOT auto-approved.
SELECT r.DealNumber AS [شماره پرونده], r.FactorNumber AS [شماره فاکتور],
       r.FactorItemRow AS [ردیف قلم], r.ProductSmallName AS [عنوان کوتاه کالا],
       r.Product AS [شرح کامل کالا], r.WeightCount AS [مقدار], r.UnitTxt AS [واحد],
       r.IssueTypeHint AS [نوع مغایرت احتمالی],
       r.PositiveQuestionTitles AS [سؤال‌های مثبت قدیمی],
       r.NumericHits AS [تعداد اعداد مشترک],
       r.UniqueTextHits AS [واژه‌های اختصاصی مشترک],
       r.NonGenericTextHits AS [واژه‌های غیرعمومی مشترک],
       r.FactorNumberMentioned AS [شماره فاکتور در متن],
       r.QuantityMentioned AS [مقدار در متن],
       COALESCE(r.MatchedTokens,N'') AS [شواهد تطبیق],
       r.LegacyDescription AS [توضیح قدیمی],
       r.EvidenceTier AS EvidenceTier,
       CASE
           WHEN r.ExistingItemReviewId IS NOT NULL THEN N'دارای نتیجه قبلی؛ انتقال ممنوع'
           WHEN r.EvidenceTier=3 THEN N'پیشنهاد قوی‌تر؛ باز هم تایید دستی'
           WHEN r.EvidenceTier=2 THEN N'پیشنهاد احتمالی؛ تایید دستی'
           ELSE N'فقط سرنخ ضعیف؛ پیشنهاد قطعی نیست'
       END AS [وضعیت],
       SUM(CASE WHEN r.EvidenceTier>=2 THEN 1 ELSE 0 END)
           OVER (PARTITION BY r.LegacyQualityControlId) AS [تعداد پیشنهادهای پرونده],
       r.LegacyQualityControlId,r.DealId,r.FactorId,r.FactorItemId,
       r.ExistingItemReviewId,r.ExistingItemReviewIsDeleted
FROM #RankedLines AS r
WHERE r.EvidenceTier>0
ORDER BY r.DealNumber DESC,r.EvidenceTier DESC,r.FactorNumber DESC,r.FactorItemRow;

-- Report 3: all invoice lines of deals with NO meaningful suggestions.
-- Use this to find the actual line manually when note lacks specific product detail.
SELECT r.DealNumber AS [شماره پرونده],
       r.FactorNumber AS [شماره فاکتور],r.FactorItemRow AS [ردیف قلم],
       r.ProductSmallName AS [عنوان کوتاه کالا],r.Product AS [شرح کامل کالا],
       r.WeightCount AS [مقدار],r.UnitTxt AS [واحد],
       r.LegacyDescription AS [توضیح کنترل کیفیت],
       r.FactorItemId,r.LegacyQualityControlId,
       r.ExistingItemReviewId
FROM #RankedLines AS r
WHERE NOT EXISTS
(
    SELECT 1 FROM #RankedLines AS other
    WHERE other.LegacyQualityControlId=r.LegacyQualityControlId
      AND other.EvidenceTier>=2
)
ORDER BY r.DealNumber DESC,r.FactorNumber DESC,r.FactorItemRow;

-- Report 4: comparable totals against the previous preview (14 / 4 / 16).
SELECT (SELECT COUNT(*) FROM #LegacyDiscrepancies) AS DiscrepantLegacyDeals,
       (SELECT COUNT(DISTINCT LegacyQualityControlId) FROM #RankedLines WHERE EvidenceTier>=2)
          AS DealsWithTextCandidates,
       (SELECT COUNT(*) FROM #LegacyDiscrepancies AS l
        WHERE NOT EXISTS
        (SELECT 1 FROM #RankedLines AS r
         WHERE r.LegacyQualityControlId=l.LegacyQualityControlId AND r.EvidenceTier>=2))
          AS DealsWithoutTextCandidates,
       (SELECT COUNT(*) FROM #RankedLines WHERE EvidenceTier>=2) AS SuggestedInvoiceItems,
       (SELECT COUNT(*) FROM #RankedLines WHERE EvidenceTier=1) AS WeakHintItems,
       (SELECT COUNT(*) FROM #RankedLines WHERE EvidenceTier>=2
           AND ExistingItemReviewId IS NOT NULL) AS AlreadyReviewedSuggestions;

DROP TABLE IF EXISTS #RankedLines;
DROP TABLE IF EXISTS #MatchedTokens;
DROP TABLE IF EXISTS #TokenUsage;
DROP TABLE IF EXISTS #CommentTokens;
DROP TABLE IF EXISTS #ProductTokens;
DROP TABLE IF EXISTS #InvoiceLines;
DROP TABLE IF EXISTS #LegacyDiscrepancies;
