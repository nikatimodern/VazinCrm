using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507011731 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplyFactorItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Product = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductCode = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductSmallName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeightCount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    UnitTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitAmount = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: false),
                    Printable = table.Column<byte>(type: "tinyint", nullable: true),
                    SupplyFactorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_SupplyFactorItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyFactorItems_SupplyFactors_SupplyFactorId",
                        column: x => x.SupplyFactorId,
                        principalTable: "SupplyFactors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyFactorItems_SupplyFactorId",
                table: "SupplyFactorItems",
                column: "SupplyFactorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplyFactorItems");
        }
    }
}
