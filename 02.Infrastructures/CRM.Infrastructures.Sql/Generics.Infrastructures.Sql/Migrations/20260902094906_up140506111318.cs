using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140506111318 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseTransactions_WarehouseProducts_WarehouseProductsId",
                table: "WarehouseTransactions");

            migrationBuilder.RenameColumn(
                name: "WarehouseProductsId",
                table: "WarehouseTransactions",
                newName: "WarehouseProductId");

            migrationBuilder.RenameIndex(
                name: "IX_WarehouseTransactions_WarehouseProductsId",
                table: "WarehouseTransactions",
                newName: "IX_WarehouseTransactions_WarehouseProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseTransactions_WarehouseProducts_WarehouseProductId",
                table: "WarehouseTransactions",
                column: "WarehouseProductId",
                principalTable: "WarehouseProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseTransactions_WarehouseProducts_WarehouseProductId",
                table: "WarehouseTransactions");

            migrationBuilder.RenameColumn(
                name: "WarehouseProductId",
                table: "WarehouseTransactions",
                newName: "WarehouseProductsId");

            migrationBuilder.RenameIndex(
                name: "IX_WarehouseTransactions_WarehouseProductId",
                table: "WarehouseTransactions",
                newName: "IX_WarehouseTransactions_WarehouseProductsId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseTransactions_WarehouseProducts_WarehouseProductsId",
                table: "WarehouseTransactions",
                column: "WarehouseProductsId",
                principalTable: "WarehouseProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
