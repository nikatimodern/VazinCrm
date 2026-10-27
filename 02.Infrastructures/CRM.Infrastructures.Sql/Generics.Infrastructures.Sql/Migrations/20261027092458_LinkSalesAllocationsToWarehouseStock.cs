using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations
{
    /// <inheritdoc />
    public partial class LinkSalesAllocationsToWarehouseStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceWarehouseProductId",
                table: "SupplyFactorItemAllocations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyFactorItemAllocations_SourceWarehouseProductId",
                table: "SupplyFactorItemAllocations",
                column: "SourceWarehouseProductId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SupplyFactorItemAllocations_WarehouseSource",
                table: "SupplyFactorItemAllocations",
                sql: "[SourceWarehouseProductId] IS NULL OR ([SupplyFactorItemId] IS NOT NULL AND [FactorItemId] IS NOT NULL AND [WarehouseId] IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyFactorItemAllocations_WarehouseProducts_SourceWarehouseProductId",
                table: "SupplyFactorItemAllocations",
                column: "SourceWarehouseProductId",
                principalTable: "WarehouseProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplyFactorItemAllocations_WarehouseProducts_SourceWarehouseProductId",
                table: "SupplyFactorItemAllocations");

            migrationBuilder.DropIndex(
                name: "IX_SupplyFactorItemAllocations_SourceWarehouseProductId",
                table: "SupplyFactorItemAllocations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SupplyFactorItemAllocations_WarehouseSource",
                table: "SupplyFactorItemAllocations");

            migrationBuilder.DropColumn(
                name: "SourceWarehouseProductId",
                table: "SupplyFactorItemAllocations");
        }
    }
}
