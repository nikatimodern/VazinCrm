using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140506181438 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "WarehouseVouchers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseVouchers_WarehouseId",
                table: "WarehouseVouchers",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseVouchers_Warehouses_WarehouseId",
                table: "WarehouseVouchers",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseVouchers_Warehouses_WarehouseId",
                table: "WarehouseVouchers");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseVouchers_WarehouseId",
                table: "WarehouseVouchers");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "WarehouseVouchers");
        }
    }
}
