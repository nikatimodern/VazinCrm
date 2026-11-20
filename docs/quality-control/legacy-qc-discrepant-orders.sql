/*
  READ ONLY — list all order numbers belonging to legacy QC deals with a discrepancy.
  Run against the CRM SQL Server database (SSMS).
  Result: one row per OrderHead; a deal with no OrderHead is still returned with NULL OrderNumber.

  Discrepancy = legacy Status = 2 OR any non-deleted legacy answer = 1.
  This deliberately includes conflicting Status = 1 / Answer = 1 cases.
  The backfill does NOT touch these deals.
*/
SET NOCOUNT ON;

;WITH DiscrepantLegacy AS
(
    SELECT qc.Id AS LegacyQualityControlId,
           qc.DealId,
           qc.[Status] AS LegacyStatus,
           qc.[Description] AS LegacyDescription,
           qc.CreateDate AS ReviewedOn,
           (
               SELECT COUNT(*)
               FROM dbo.QualityControlAnswers AS answer
               WHERE answer.QualityControlId = qc.Id
                 AND answer.IsDeleted = 0 AND answer.Answer = 1
           ) AS PositiveAnswerCount
    FROM dbo.QualityControls AS qc
    WHERE qc.IsDeleted = 0
      AND
      (
          qc.[Status] = 2
          OR EXISTS
          (
              SELECT 1
              FROM dbo.QualityControlAnswers AS answer
              WHERE answer.QualityControlId = qc.Id
                AND answer.IsDeleted = 0 AND answer.Answer = 1
          )
      )
)
SELECT d.SerialNumber AS DealNumber,
       oh.OrderNumber AS OrderNumber,
       d.Title AS DealTitle,
       d.CustomerName AS CustomerName,
       legacy.LegacyStatus,
       legacy.PositiveAnswerCount,
       legacy.LegacyDescription,
       legacy.ReviewedOn,
       d.Id AS DealId,
       oh.Id AS OrderHeadId,
       legacy.LegacyQualityControlId
FROM DiscrepantLegacy AS legacy
INNER JOIN dbo.Deals AS d ON d.Id = legacy.DealId AND d.IsDeleted = 0
LEFT JOIN dbo.OrderHeads AS oh ON oh.DealId = d.Id AND oh.IsDeleted = 0
ORDER BY d.SerialNumber DESC, oh.OrderNumber DESC;
