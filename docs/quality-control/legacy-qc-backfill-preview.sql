/*
  READ-ONLY PREVIEW — before running EF migration
  20261119190000_BackfillLegacyQualityControlItems.

  ONLY old QC with no discrepancy is eligible:
    Status = 1, or NULL with at least one non-deleted saved answer;
    in BOTH cases, there must be NO saved answer = 1.
  Status = 2 or any saved answer = 1 is excluded from the backfill.
  Status 4/5 and NULL without saved answers are not completed reviews.
  The original legacy rows and existing item reviews are never modified.

  For the actual order numbers needing manual review run:
  docs/quality-control/legacy-qc-discrepant-orders.sql

  No persistent data is modified by this script.
*/
SET NOCOUNT ON;

;WITH LegacyRows AS
(
    SELECT qc.Id AS LegacyQualityControlId,
           qc.DealId,
           qc.[Status] AS LegacyStatus,
           d.SerialNumber AS DealNumber,
           d.Title AS DealTitle,
           d.FinancialStatus,
           (
               SELECT COUNT(*)
               FROM dbo.QualityControlAnswers AS a
               WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0
           ) AS SavedAnswers,
           (
               SELECT COUNT(*)
               FROM dbo.QualityControlAnswers AS a
               WHERE a.QualityControlId = qc.Id
                 AND a.IsDeleted = 0 AND a.Answer = 1
           ) AS PositiveAnswers
    FROM dbo.QualityControls AS qc
    INNER JOIN dbo.Deals AS d ON d.Id = qc.DealId
    WHERE qc.IsDeleted = 0 AND d.IsDeleted = 0
),
Preflight AS
(
    SELECT l.*,
           CASE WHEN l.LegacyStatus = 1
                     OR (l.LegacyStatus IS NULL AND l.SavedAnswers > 0)
                THEN 1 ELSE 0 END AS WasReviewed,
           CASE WHEN l.LegacyStatus = 2 OR l.PositiveAnswers > 0
                THEN 1 ELSE 0 END AS HadDiscrepancy,
           COALESCE(lines.EligibleItems, 0) AS EligibleItems,
           COALESCE(lines.AlreadyReviewed, 0) AS ExistingItemReviews
    FROM LegacyRows AS l
    OUTER APPLY
    (
        SELECT COUNT(*) AS EligibleItems,
               COALESCE(SUM(CASE WHEN review.Id IS NULL THEN 0 ELSE 1 END), 0)
                   AS AlreadyReviewed
        FROM dbo.Factors AS f
        INNER JOIN dbo.FactorItems AS fi ON fi.[factorId] = f.Id AND fi.IsDeleted = 0
        LEFT JOIN dbo.QualityControlItemReviews AS review ON review.FactorItemId = fi.Id
        WHERE f.DealId = l.DealId
          AND f.IsDeleted = 0 AND f.[status] = 2 AND f.Returned = 0
    ) AS lines
)
SELECT LegacyQualityControlId, DealId, DealNumber, DealTitle,
       LegacyStatus, SavedAnswers, PositiveAnswers,
       CASE WHEN HadDiscrepancy = 1 THEN N'دارای مغایرت؛ بدون انتقال؛ بازبینی دستی'
            WHEN WasReviewed = 0 THEN N'بررسی تکمیل نشده یا وضعیت تماس؛ بدون انتقال'
            WHEN FinancialStatus <> 1 THEN N'تایید مالی نشده؛ بدون انتقال'
            ELSE N'بدون مغایرت؛ آماده بک‌فیل'
       END AS BackfillDecision,
       EligibleItems, ExistingItemReviews,
       CASE WHEN HadDiscrepancy = 0 AND WasReviewed = 1 AND FinancialStatus = 1
            THEN EligibleItems - ExistingItemReviews ELSE 0 END AS WouldInsert
FROM Preflight
ORDER BY WouldInsert DESC, DealNumber DESC;

-- Totals calculated using exactly the safe criteria of the migration:
;WITH SafeLegacy AS
(
    SELECT qc.Id AS LegacyQualityControlId, qc.DealId
    FROM dbo.QualityControls AS qc
    INNER JOIN dbo.Deals AS d ON d.Id = qc.DealId
    WHERE qc.IsDeleted = 0 AND d.IsDeleted = 0 AND d.FinancialStatus = 1
      AND
      (
          qc.[Status] = 1
          OR (qc.[Status] IS NULL AND EXISTS
              (
                  SELECT 1 FROM dbo.QualityControlAnswers AS a
                  WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0
              ))
      )
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.QualityControlAnswers AS a
          WHERE a.QualityControlId = qc.Id
            AND a.IsDeleted = 0 AND a.Answer = 1
      )
),
Eligible AS
(
    SELECT old.LegacyQualityControlId,
           fi.Id AS FactorItemId,
           review.Id AS ExistingItemReviewId
    FROM SafeLegacy AS old
    INNER JOIN dbo.Factors AS f ON f.DealId = old.DealId
        AND f.IsDeleted = 0 AND f.[status] = 2 AND f.Returned = 0
    INNER JOIN dbo.FactorItems AS fi ON fi.[factorId] = f.Id
        AND fi.IsDeleted = 0
    LEFT JOIN dbo.QualityControlItemReviews AS review
        ON review.FactorItemId = fi.Id
)
SELECT COUNT(DISTINCT LegacyQualityControlId) AS EligibleLegacyDeals,
       COUNT(*) AS EligibleInvoiceItems,
       COALESCE(SUM(CASE WHEN ExistingItemReviewId IS NULL THEN 1 ELSE 0 END), 0)
           AS PendingInsertions,
       COALESCE(SUM(CASE WHEN ExistingItemReviewId IS NOT NULL THEN 1 ELSE 0 END), 0)
           AS PreservedItemReviews
FROM Eligible;
