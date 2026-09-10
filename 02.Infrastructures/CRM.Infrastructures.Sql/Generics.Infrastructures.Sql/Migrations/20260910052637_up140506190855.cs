using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140506190855 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseVoucherId",
                table: "WarehouseTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransactions_WarehouseVoucherId",
                table: "WarehouseTransactions",
                column: "WarehouseVoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseTransactions_WarehouseVouchers_WarehouseVoucherId",
                table: "WarehouseTransactions",
                column: "WarehouseVoucherId",
                principalTable: "WarehouseVouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseTransactions_WarehouseVouchers_WarehouseVoucherId",
                table: "WarehouseTransactions");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseTransactions_WarehouseVoucherId",
                table: "WarehouseTransactions");

            migrationBuilder.DropColumn(
                name: "WarehouseVoucherId",
                table: "WarehouseTransactions");
        }
    }
}
