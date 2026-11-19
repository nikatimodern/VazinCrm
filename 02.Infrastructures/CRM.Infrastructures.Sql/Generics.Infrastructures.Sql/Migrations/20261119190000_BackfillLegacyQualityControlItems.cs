using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations;

/// <summary>
/// One-time, additive migration of only discrepancy-free legacy deal reviews
/// into per-invoice-line reviews. Historical discrepancies are not migrated.
/// </summary>
[DbContext(typeof(CRMContextDb))]
[Migration("20261119190000_BackfillLegacyQualityControlItems")]
public partial class BackfillLegacyQualityControlItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The migration runner executes this SQL in its transaction.
        // Only confirmed discrepancy-free legacy reviews qualify:
        // Status 1, or NULL with saved question answers, and NO answer = 1.
        // Status 2, any positive answer, contact statuses 4/5 and incomplete
        // NULL rows are excluded. Ambiguous status/answer combinations are
        // deliberately excluded and listed in the manual-review report.
        // Existing item reviews (including soft-deleted ones) are never overwritten.
        migrationBuilder.Sql(@"
;WITH LegacyReviewed AS
(
    SELECT qc.Id AS LegacyQualityControlId,
           qc.DealId,
           qc.CreateDate, qc.ModifyDate,
           qc.CreateDateTxt, qc.ModifyDateTxt,
           qc.UserCreatorID, qc.UserCreator,
           qc.UserModifyID, qc.UserModify
    FROM dbo.QualityControls AS qc
    INNER JOIN dbo.Deals AS d ON d.Id = qc.DealId
    WHERE qc.IsDeleted = 0
      AND d.IsDeleted = 0
      AND d.FinancialStatus = 1
      AND
      (
          qc.Status = 1
          OR
          (
              qc.Status IS NULL
              AND EXISTS
                  (SELECT 1 FROM dbo.QualityControlAnswers AS a
                   WHERE a.QualityControlId = qc.Id AND a.IsDeleted = 0)
          )
      )
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.QualityControlAnswers AS a
          WHERE a.QualityControlId = qc.Id
            AND a.IsDeleted = 0 AND a.Answer = 1
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
           N'بک‌فیل کنترل کیفیت قدیمی بدون مغایرت؛ شناسه رکورد اصلی: ',
           CONVERT(nvarchar(36), source.LegacyQualityControlId)
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
