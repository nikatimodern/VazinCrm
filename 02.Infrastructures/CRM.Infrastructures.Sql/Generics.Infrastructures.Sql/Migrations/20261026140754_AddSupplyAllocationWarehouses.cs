using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations
{
    /// <inheritdoc />
    public partial class AddSupplyAllocationWarehouses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Weight", table: "WarehouseProductVouchers", type: "decimal(20,2)",
                precision: 20, scale: 2, nullable: false,
                oldClrType: typeof(decimal), oldType: "decimal(20,0)", oldPrecision: 20);

            migrationBuilder.DropIndex(
                name: "IX_WarehouseProducts_WarehouseId",
                table: "WarehouseProducts");

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "WarehouseTransactions",
                type: "decimal(20,2)",
                precision: 20,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldPrecision: 20);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplyFactorItemId",
                table: "WarehouseTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplyFactorItemId",
                table: "WarehouseProducts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FactorItemId",
                table: "SupplyFactorItemAllocations",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "SupplyFactorItemAllocations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransactions_SupplyFactorItemId",
                table: "WarehouseTransactions",
                column: "SupplyFactorItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseProducts_SupplyFactorItemId",
                table: "WarehouseProducts",
                column: "SupplyFactorItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseProducts_WarehouseId_SupplyFactorItemId",
                table: "WarehouseProducts",
                columns: new[] { "WarehouseId", "SupplyFactorItemId" },
                unique: true,
                filter: "[SupplyFactorItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyFactorItemAllocations_WarehouseId",
                table: "SupplyFactorItemAllocations",
                column: "WarehouseId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SupplyFactorItemAllocations_Destination",
                table: "SupplyFactorItemAllocations",
                sql: "([FactorItemId] IS NOT NULL AND [WarehouseId] IS NULL) OR ([FactorItemId] IS NULL AND [WarehouseId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyFactorItemAllocations_Warehouses_WarehouseId",
                table: "SupplyFactorItemAllocations",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseProducts_SupplyFactorItems_SupplyFactorItemId",
                table: "WarehouseProducts",
                column: "SupplyFactorItemId",
                principalTable: "SupplyFactorItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseTransactions_SupplyFactorItems_SupplyFactorItemId",
                table: "WarehouseTransactions",
                column: "SupplyFactorItemId",
                principalTable: "SupplyFactorItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""

-- The three system warehouses are distinct from ordinary, user-managed warehouses.
INSERT INTO [Warehouses] ([Id], [Title], [WarehouseType], [CreateDate], [ModifyDate], [IsDeleted], [UserCreator])
SELECT seed.Id, seed.Title, seed.WarehouseType, GETDATE(), GETDATE(), 0, N'سیستم'
FROM (VALUES
    (CAST('a732c1a0-8710-4b2f-9501-000000000001' AS uniqueidentifier), N'انبار ضایعات', 0),
    (CAST('a732c1a0-8710-4b2f-9501-000000000002' AS uniqueidentifier), N'انبار گردشی', 1),
    (CAST('a732c1a0-8710-4b2f-9501-000000000003' AS uniqueidentifier), N'انبار ذخیره', 2)
) seed(Id, Title, WarehouseType)
WHERE NOT EXISTS (SELECT 1 FROM [Warehouses] w WHERE w.Id = seed.Id);

IF EXISTS (
    SELECT 1 FROM [SupplyFactorItems] i
    JOIN [SupplyFactors] f ON f.Id = i.SupplyFactorId
    WHERE i.IsDeleted = 0 AND f.IsDeleted = 0
      AND (i.WeightCount < 0 OR i.WeightCount < (
          SELECT COALESCE(SUM(a.Weight), 0) FROM [SupplyFactorItemAllocations] a WHERE a.SupplyFactorItemId = i.Id))
)
    THROW 51000, N'مقدار آیتم تامین از مجموع تخصیص‌های قبلی کمتر است؛ ابتدا داده‌ها را اصلاح کنید.', 1;

-- Backfill each active supply item's receipt and its already allocated outgoing quantity.
INSERT INTO [WarehouseProducts] (
    [Id], [WarehouseId], [SupplyFactorItemId], [ProductCode], [ProductSmallName], [ProductDescription],
    [Unit], [UnitTxt], [CreateDate], [ModifyDate], [IsDeleted], [UserCreatorID], [UserCreator])
SELECT NEWID(), 'a732c1a0-8710-4b2f-9501-000000000002', i.Id, p.Id, i.ProductSmallName, i.Product,
    i.Unit, i.UnitTxt, GETDATE(), GETDATE(), 0, i.UserCreatorID, i.UserCreator
FROM [SupplyFactorItems] i
JOIN [SupplyFactors] f ON f.Id = i.SupplyFactorId
LEFT JOIN [Products] p ON p.Id = i.ProductCode
WHERE i.IsDeleted = 0 AND f.IsDeleted = 0 AND i.WeightCount > 0
  AND NOT EXISTS (SELECT 1 FROM [WarehouseProducts] wp
      WHERE wp.SupplyFactorItemId = i.Id AND wp.WarehouseId = 'a732c1a0-8710-4b2f-9501-000000000002');

INSERT INTO [WarehouseTransactions] (
    [Id], [WarehouseProductId], [SupplyFactorItemId], [Weight], [TransactionType],
    [CreateDate], [ModifyDate], [IsDeleted], [UserCreatorID], [UserCreator], [CommentText])
SELECT NEWID(), wp.Id, i.Id, i.WeightCount, 1, GETDATE(), GETDATE(), 0,
    i.UserCreatorID, i.UserCreator, N'ورود اولیه فاکتور تامین — انتقال سوابق قبلی'
FROM [WarehouseProducts] wp
JOIN [SupplyFactorItems] i ON i.Id = wp.SupplyFactorItemId
WHERE wp.WarehouseId = 'a732c1a0-8710-4b2f-9501-000000000002';

INSERT INTO [WarehouseTransactions] (
    [Id], [WarehouseProductId], [SupplyFactorItemId], [Weight], [TransactionType],
    [CreateDate], [ModifyDate], [IsDeleted], [UserCreator], [CommentText])
SELECT NEWID(), wp.Id, wp.SupplyFactorItemId, SUM(a.Weight), 0, GETDATE(), GETDATE(), 0,
    N'سیستم', N'خروج بابت تخصیص‌های قبلی فروش'
FROM [WarehouseProducts] wp
JOIN [SupplyFactorItemAllocations] a ON a.SupplyFactorItemId = wp.SupplyFactorItemId
WHERE wp.WarehouseId = 'a732c1a0-8710-4b2f-9501-000000000002'
GROUP BY wp.Id, wp.SupplyFactorItemId;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM [WarehouseTransactions] WHERE [SupplyFactorItemId] IS NOT NULL)
 OR EXISTS (SELECT 1 FROM [SupplyFactorItemAllocations] WHERE [WarehouseId] IS NOT NULL)
    THROW 51000, N'بازگشت این Migration با وجود گردش تامین مجاز نیست؛ ابتدا سوابق را با روش مصوب انتقال دهید.', 1;
""");

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight", table: "WarehouseProductVouchers", type: "decimal(20,0)",
                precision: 20, nullable: false,
                oldClrType: typeof(decimal), oldType: "decimal(20,2)", oldPrecision: 20, oldScale: 2);

            migrationBuilder.DropForeignKey(
                name: "FK_SupplyFactorItemAllocations_Warehouses_WarehouseId",
                table: "SupplyFactorItemAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseProducts_SupplyFactorItems_SupplyFactorItemId",
                table: "WarehouseProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseTransactions_SupplyFactorItems_SupplyFactorItemId",
                table: "WarehouseTransactions");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseTransactions_SupplyFactorItemId",
                table: "WarehouseTransactions");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseProducts_SupplyFactorItemId",
                table: "WarehouseProducts");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseProducts_WarehouseId_SupplyFactorItemId",
                table: "WarehouseProducts");

            migrationBuilder.DropIndex(
                name: "IX_SupplyFactorItemAllocations_WarehouseId",
                table: "SupplyFactorItemAllocations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SupplyFactorItemAllocations_Destination",
                table: "SupplyFactorItemAllocations");

            migrationBuilder.DropColumn(
                name: "SupplyFactorItemId",
                table: "WarehouseTransactions");

            migrationBuilder.DropColumn(
                name: "SupplyFactorItemId",
                table: "WarehouseProducts");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "SupplyFactorItemAllocations");

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "WarehouseTransactions",
                type: "decimal(20,0)",
                precision: 20,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,2)",
                oldPrecision: 20,
                oldScale: 2);

            migrationBuilder.AlterColumn<Guid>(
                name: "FactorItemId",
                table: "SupplyFactorItemAllocations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseProducts_WarehouseId",
                table: "WarehouseProducts",
                column: "WarehouseId");
        }
    }
}
