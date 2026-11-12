using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations;

[DbContext(typeof(CRMContextDb))]
[Migration("20261112130000_UnifyRequestAmountTargetPerson")]
public partial class UnifyRequestAmountTargetPerson : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "TargetPersonId",
            table: "RequestAmounts",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TargetPersonTxt",
            table: "RequestAmounts",
            type: "nvarchar(max)",
            nullable: true);

        // Existing petty-cash requests stored Cashier.UserId instead of Cashier.Id.
        // Convert those values to the actual Cashier primary key before dropping the old fields.
        // Refuse ambiguous/unmapped old data rather than silently deleting an association.
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1
                FROM dbo.RequestAmounts AS r
                WHERE r.RequestAmountType = 3
                  AND r.CashierId IS NOT NULL
                  AND (SELECT COUNT(*) FROM dbo.Cashiers AS c
                       WHERE c.Id = r.CashierId OR c.UserId = r.CashierId) <> 1
            )
            BEGIN
                THROW 50002, 'A petty cash request has an unmapped or ambiguous CashierId. Resolve it before migrating.', 1;
            END;

            IF EXISTS (
                SELECT 1
                FROM dbo.RequestAmounts AS r
                WHERE r.RequestAmountType <> 3
                  AND r.SupplierId IS NOT NULL
                  AND r.CashierId IS NOT NULL
            )
            BEGIN
                THROW 50003, 'A request has both supplier and cashier IDs. Resolve the conflicting target before migrating.', 1;
            END;

            UPDATE r
            SET r.TargetPersonId = CASE WHEN r.RequestAmountType = 3 THEN c.Id ELSE r.SupplierId END,
                r.TargetPersonTxt = CASE WHEN r.RequestAmountType = 3 THEN r.CashierTxt ELSE r.SupplierTxt END
            FROM dbo.RequestAmounts AS r
            LEFT JOIN dbo.Cashiers AS c
                ON r.RequestAmountType = 3 AND (c.UserId = r.CashierId OR c.Id = r.CashierId);
            """);

        migrationBuilder.CreateIndex(
            name: "IX_RequestAmounts_TargetPersonId",
            table: "RequestAmounts",
            column: "TargetPersonId");

        migrationBuilder.DropColumn(name: "SupplierId", table: "RequestAmounts");
        migrationBuilder.DropColumn(name: "SupplierTxt", table: "RequestAmounts");
        migrationBuilder.DropColumn(name: "CashierId", table: "RequestAmounts");
        migrationBuilder.DropColumn(name: "CashierTxt", table: "RequestAmounts");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "SupplierId",
            table: "RequestAmounts",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SupplierTxt",
            table: "RequestAmounts",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "CashierId",
            table: "RequestAmounts",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CashierTxt",
            table: "RequestAmounts",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE r
            SET r.SupplierId = CASE WHEN r.RequestAmountType = 3 THEN NULL ELSE r.TargetPersonId END,
                r.SupplierTxt = CASE WHEN r.RequestAmountType = 3 THEN NULL ELSE r.TargetPersonTxt END,
                r.CashierId = CASE WHEN r.RequestAmountType = 3 THEN c.UserId ELSE NULL END,
                r.CashierTxt = CASE WHEN r.RequestAmountType = 3 THEN r.TargetPersonTxt ELSE NULL END
            FROM dbo.RequestAmounts AS r
            LEFT JOIN dbo.Cashiers AS c ON r.RequestAmountType = 3 AND c.Id = r.TargetPersonId;
            """);

        migrationBuilder.DropIndex(
            name: "IX_RequestAmounts_TargetPersonId",
            table: "RequestAmounts");
        migrationBuilder.DropColumn(name: "TargetPersonId", table: "RequestAmounts");
        migrationBuilder.DropColumn(name: "TargetPersonTxt", table: "RequestAmounts");
    }
}
