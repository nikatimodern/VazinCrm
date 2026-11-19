/*
  READ-ONLY PREVIEW: legacy deal QC -> new invoice-item QC.
  Execute in the target CRM database before applying migration
  20261119190000_BackfillLegacyQualityControlItems.

  Old deal QC statuses:
    1 = no discrepancy, 2 = discrepancy, 4/5 = contact outcome.
    NULL with at least one non-deleted answer = completed old QC.
  Backfill marks every eligible invoice item as status 1 (no discrepancy),
  *including* old deals with a discrepancy: the affected item is unknown.
  Existing item reviews (even soft-deleted) will not be overwritten.

  This script DOES NOT change any persistent data.
*/
SET NOCOUNT ON;

;WITH LegacyRows AS
(
    SELECT qc.Id AS LegacyQualityControlId,
           qc.DealId, qc.[Status] AS LegacyStatus,
           d.SerialNumber AS DealSerial,
           d.FinancialStatus,
           (SELECT COUNT(*) FROM dbo.QualityControlAnswers AS a
             WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0) AS SavedAnswers,
           (SELECT COUNT(*) FROM dbo.QualityControlAnswers AS a
             WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0 AND a.Answer = 1) AS DiscrepantAnswers
    FROM dbo.QualityControls AS qc
    INNER JOIN dbo.Deals AS d ON d.Id = qc.DealId
    WHERE qc.IsDeleted = 0 AND d.IsDeleted = 0
),
Preflight AS
(
    SELECT l.*,
           CASE WHEN l.LegacyStatus IN (1, 2)
                 OR (l.LegacyStatus IS NULL AND l.SavedAnswers > 0)
                THEN 1 ELSE 0 END AS IsCompletedLegacyQc,
           CASE WHEN l.LegacyStatus = 2 OR l.DiscrepantAnswers > 0
                THEN 1 ELSE 0 END AS HadLegacyDiscrepancy,
           COALESCE(lines.EligibleItems, 0) AS EligibleItems,
           COALESCE(lines.AlreadyReviewed, 0) AS AlreadyReviewed,
           COALESCE(lines.EligibleItems, 0) - COALESCE(lines.AlreadyReviewed, 0) AS AvailableItems
    FROM LegacyRows AS l
    OUTER APPLY
    (
        SELECT COUNT(*) AS EligibleItems,
               COALESCE(SUM(CASE WHEN review.Id IS NOT NULL THEN 1 ELSE 0 END), 0) AS AlreadyReviewed
        FROM dbo.Factors AS f
        INNER JOIN dbo.FactorItems AS fi ON fi.[factorId] = f.Id AND fi.IsDeleted = 0
        LEFT JOIN dbo.QualityControlItemReviews AS review ON review.FactorItemId = fi.Id
        WHERE f.DealId = l.DealId
          AND f.IsDeleted = 0 AND f.[status] = 2 AND f.Returned = 0
    ) AS lines
)
SELECT LegacyQualityControlId, DealId, DealSerial,
       LegacyStatus, SavedAnswers, DiscrepantAnswers,
       CASE WHEN IsCompletedLegacyQc = 0 THEN N'بررسی قدیمی تکمیل نشده یا وضعیت تماس'
            WHEN FinancialStatus <> 1 THEN N'پرونده مالی تایید نشده'
            WHEN HadLegacyDiscrepancy = 1 THEN N'بک‌فیل بدون مغایرت (سفارش قدیمی مغایرت داشته)'
            ELSE N'بک‌فیل بدون مغایرت (سفارش قدیمی سالم)'
       END AS BackfillDecision,
       EligibleItems, AlreadyReviewed,
       CASE WHEN IsCompletedLegacyQc = 1 AND FinancialStatus = 1
            THEN AvailableItems ELSE 0 END AS WouldInsert
FROM Preflight
ORDER BY WouldInsert DESC, DealSerial DESC;

-- Totals for the same eligibility rules as the migration:
;WITH LegacyReviewed AS
(
    SELECT qc.Id, qc.DealId,
           CASE WHEN qc.Status = 2 OR EXISTS
                 (SELECT 1 FROM dbo.QualityControlAnswers AS a
                  WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0 AND a.Answer = 1)
                THEN 1 ELSE 0 END AS HadLegacyDiscrepancy
    FROM dbo.QualityControls AS qc
    INNER JOIN dbo.Deals AS d ON d.Id = qc.DealId
    WHERE qc.IsDeleted = 0 AND d.IsDeleted = 0 AND d.FinancialStatus = 1
      AND (qc.Status IN (1, 2)
           OR (qc.Status IS NULL AND EXISTS
               (SELECT 1 FROM dbo.QualityControlAnswers AS a
                WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0)))
),
Eligible AS
(
    SELECT legacy.Id, legacy.HadLegacyDiscrepancy,
           fi.Id AS FactorItemId, review.Id AS ExistingReviewId
    FROM LegacyReviewed AS legacy
    INNER JOIN dbo.Factors AS f ON f.DealId = legacy.DealId
        AND f.IsDeleted = 0 AND f.[status] = 2 AND f.Returned = 0
    INNER JOIN dbo.FactorItems AS fi ON fi.[factorId] = f.Id AND fi.IsDeleted = 0
    LEFT JOIN dbo.QualityControlItemReviews AS review ON review.FactorItemId = fi.Id
)
SELECT COUNT(DISTINCT Id) AS EligibleLegacyDeals,
       COUNT(*) AS EligibleInvoiceItems,
       SUM(CASE WHEN ExistingReviewId IS NULL THEN 1 ELSE 0 END) AS PendingInsertions,
       SUM(CASE WHEN ExistingReviewId IS NOT NULL THEN 1 ELSE 0 END) AS PreservedItemReviews,
       SUM(CASE WHEN ExistingReviewId IS NULL AND HadLegacyDiscrepancy = 1
                THEN 1 ELSE 0 END) AS NewItemsFromDiscrepantLegacyDeals
FROM Eligible;
