using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140506111556 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseProducts_Products_ProductId",
                table: "WarehouseProducts");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "WarehouseProducts",
                newName: "ProductCode");

            migrationBuilder.RenameIndex(
                name: "IX_WarehouseProducts_ProductId",
                table: "WarehouseProducts",
                newName: "IX_WarehouseProducts_ProductCode");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseProducts_Products_ProductCode",
                table: "WarehouseProducts",
                column: "ProductCode",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseProducts_Products_ProductCode",
                table: "WarehouseProducts");

            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "WarehouseProducts",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_WarehouseProducts_ProductCode",
                table: "WarehouseProducts",
                newName: "IX_WarehouseProducts_ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseProducts_Products_ProductId",
                table: "WarehouseProducts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
