using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations;

/// <summary>
/// One-time, additive migration from the old deal-level quality control to
/// per-invoice-line reviews. Does not infer which line caused a deal-level issue.
/// </summary>
[DbContext(typeof(CRMContextDb))]
[Migration("20261119190000_BackfillLegacyQualityControlItems")]
public partial class BackfillLegacyQualityControlItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The migration runner executes this SQL in its transaction.
        // Only already-reviewed legacy deals qualify:
        //   1 = no discrepancy, 2 = discrepancy, NULL = derive from saved answers.
        // 4/5 (no answer / invalid number) and NULL without answers are excluded.
        // Both reviewed outcomes become "no discrepancy" at item level, because
        // old deal-wide discrepancies have no reliable invoice-line attribution.
        // Existing item reviews (including soft-deleted ones) are never overwritten.
        migrationBuilder.Sql(@"
;WITH LegacyReviewed AS
(
    SELECT qc.Id AS LegacyQualityControlId,
           qc.DealId,
           qc.CreateDate, qc.ModifyDate,
           qc.CreateDateTxt, qc.ModifyDateTxt,
           qc.UserCreatorID, qc.UserCreator,
           qc.UserModifyID, qc.UserModify,
           CASE WHEN qc.Status = 2 OR EXISTS
               (SELECT 1 FROM dbo.QualityControlAnswers AS a
                WHERE a.QualityControlId = qc.Id
                  AND a.IsDeleted = 0 AND a.Answer = 1)
                THEN 1 ELSE 0 END AS HadLegacyDiscrepancy
    FROM dbo.QualityControls AS qc
    INNER JOIN dbo.Deals AS d ON d.Id = qc.DealId
    WHERE qc.IsDeleted = 0
      AND d.IsDeleted = 0
      AND d.FinancialStatus = 1
      AND
      (
          qc.Status IN (1, 2)
          OR
          (
              qc.Status IS NULL
              AND EXISTS
                  (SELECT 1 FROM dbo.QualityControlAnswers AS a
                   WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0)
          )
      )
),
EligibleItems AS
(
    SELECT fi.Id AS FactorItemId,
           legacy.*
    FROM LegacyReviewed AS legacy
    INNER JOIN dbo.Factors AS f ON f.DealId = legacy.DealId
        AND f.IsDeleted = 0 AND f.[status] = 2 AND f.Returned = 0
    INNER JOIN dbo.FactorItems AS fi ON fi.[factorId] = f.Id
        AND fi.IsDeleted = 0
)
INSERT INTO dbo.QualityControlItemReviews
(
    Id, FactorItemId, [Status], [Description],
    CreateDate, ModifyDate, CreateDateTxt, ModifyDateTxt,
    IsDeleted, UserCreatorID, UserCreator, UserModifyID, UserModify, CommentText
)
SELECT NEWID(), source.FactorItemId, 1, NULL,
       source.CreateDate, COALESCE(source.ModifyDate, source.CreateDate),
       source.CreateDateTxt, COALESCE(source.ModifyDateTxt, source.CreateDateTxt),
       0, source.UserCreatorID, source.UserCreator,
       source.UserModifyID, source.UserModify,
       CONCAT(
           N'بک‌فیل کنترل کیفیت قدیمی؛ شناسه رکورد اصلی: ',
           CONVERT(nvarchar(36), source.LegacyQualityControlId),
           CASE WHEN source.HadLegacyDiscrepancy = 1
               THEN N'؛ نتیجه قدیمی: دارای مغایرت (قلم مشخص نبود؛ همه اقلام بدون مغایرت ثبت شدند).'
               ELSE N'؛ نتیجه قدیمی: بدون مغایرت.' END
       )
FROM EligibleItems AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.QualityControlItemReviews AS existing WITH (UPDLOCK, HOLDLOCK)
    WHERE existing.FactorItemId = source.FactorItemId
);
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive: deleting migrated reviews on rollback
        // could erase subsequent user edits. Preserve the original legacy rows.
    }
}
