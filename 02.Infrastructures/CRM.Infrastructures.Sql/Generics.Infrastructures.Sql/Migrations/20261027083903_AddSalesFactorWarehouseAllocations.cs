using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations
{
    /// <inheritdoc />
    public partial class AddSalesFactorWarehouseAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SupplyFactorItemAllocations_Destination",
                table: "SupplyFactorItemAllocations");

            migrationBuilder.AddColumn<Guid>(
                name: "FactorItemId",
                table: "WarehouseTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FactorItemId",
                table: "WarehouseProducts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplyFactorItemId",
                table: "SupplyFactorItemAllocations",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransactions_FactorItemId",
                table: "WarehouseTransactions",
                column: "FactorItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseProducts_FactorItemId",
                table: "WarehouseProducts",
                column: "FactorItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseProducts_WarehouseId_FactorItemId",
                table: "WarehouseProducts",
                columns: new[] { "WarehouseId", "FactorItemId" },
                unique: true,
                filter: "[FactorItemId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SupplyFactorItemAllocations_Destination",
                table: "SupplyFactorItemAllocations",
                sql: "([SupplyFactorItemId] IS NOT NULL AND [FactorItemId] IS NOT NULL AND [WarehouseId] IS NULL) OR ([SupplyFactorItemId] IS NOT NULL AND [FactorItemId] IS NULL AND [WarehouseId] IS NOT NULL) OR ([SupplyFactorItemId] IS NULL AND [FactorItemId] IS NOT NULL AND [WarehouseId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseProducts_FactorItems_FactorItemId",
                table: "WarehouseProducts",
                column: "FactorItemId",
                principalTable: "FactorItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseTransactions_FactorItems_FactorItemId",
                table: "WarehouseTransactions",
                column: "FactorItemId",
                principalTable: "FactorItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseProducts_FactorItems_FactorItemId",
                table: "WarehouseProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseTransactions_FactorItems_FactorItemId",
                table: "WarehouseTransactions");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseTransactions_FactorItemId",
                table: "WarehouseTransactions");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseProducts_FactorItemId",
                table: "WarehouseProducts");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseProducts_WarehouseId_FactorItemId",
                table: "WarehouseProducts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SupplyFactorItemAllocations_Destination",
                table: "SupplyFactorItemAllocations");

            migrationBuilder.DropColumn(
                name: "FactorItemId",
                table: "WarehouseTransactions");

            migrationBuilder.DropColumn(
                name: "FactorItemId",
                table: "WarehouseProducts");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplyFactorItemId",
                table: "SupplyFactorItemAllocations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SupplyFactorItemAllocations_Destination",
                table: "SupplyFactorItemAllocations",
                sql: "([FactorItemId] IS NOT NULL AND [WarehouseId] IS NULL) OR ([FactorItemId] IS NULL AND [WarehouseId] IS NOT NULL)");
        }
    }
}
