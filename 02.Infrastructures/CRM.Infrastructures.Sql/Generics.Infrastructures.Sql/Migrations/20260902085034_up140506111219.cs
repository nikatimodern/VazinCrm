using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140506111219 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseTransactions_Warehouses_WarehouseId",
                table: "WarehouseTransactions");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "WarehouseTransactions");

            migrationBuilder.DropColumn(
                name: "ProductTxt",
                table: "WarehouseTransactions");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "WarehouseTransactions");

            migrationBuilder.DropColumn(
                name: "UnitTxt",
                table: "WarehouseTransactions");

            migrationBuilder.RenameColumn(
                name: "WarehouseId",
                table: "WarehouseTransactions",
                newName: "WarehouseProductsId");

            migrationBuilder.RenameIndex(
                name: "IX_WarehouseTransactions_WarehouseId",
                table: "WarehouseTransactions",
                newName: "IX_WarehouseTransactions_WarehouseProductsId");

            migrationBuilder.CreateTable(
                name: "WarehouseProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductSmallName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    UnitTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UserCreatorID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserCreator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModifyID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModify = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseProducts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseProducts_ProductId",
                table: "WarehouseProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseProducts_WarehouseId",
                table: "WarehouseProducts",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseTransactions_WarehouseProducts_WarehouseProductsId",
                table: "WarehouseTransactions",
                column: "WarehouseProductsId",
                principalTable: "WarehouseProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseTransactions_WarehouseProducts_WarehouseProductsId",
                table: "WarehouseTransactions");

            migrationBuilder.DropTable(
                name: "WarehouseProducts");

            migrationBuilder.RenameColumn(
                name: "WarehouseProductsId",
                table: "WarehouseTransactions",
                newName: "WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_WarehouseTransactions_WarehouseProductsId",
                table: "WarehouseTransactions",
                newName: "IX_WarehouseTransactions_WarehouseId");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "WarehouseTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductTxt",
                table: "WarehouseTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Unit",
                table: "WarehouseTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UnitTxt",
                table: "WarehouseTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseTransactions_Warehouses_WarehouseId",
                table: "WarehouseTransactions",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
