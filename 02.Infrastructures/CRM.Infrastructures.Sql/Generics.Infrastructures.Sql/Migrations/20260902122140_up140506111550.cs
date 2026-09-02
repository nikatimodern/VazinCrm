using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140506111550 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseProducts_Products_ProductId",
                table: "WarehouseProducts");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseProducts_Products_ProductId",
                table: "WarehouseProducts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseProducts_Products_ProductId",
                table: "WarehouseProducts");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseProducts_Products_ProductId",
                table: "WarehouseProducts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
