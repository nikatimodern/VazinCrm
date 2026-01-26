using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up14040513 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "PreFactorItems");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                table: "PreFactorItems");

            migrationBuilder.CreateTable(
                name: "Supplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PfItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_Supplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Supplies_PreFactorItems_PfItemId",
                        column: x => x.PfItemId,
                        principalTable: "PreFactorItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Supplies_PfItemId",
                table: "Supplies",
                column: "PfItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Supplies");

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "PreFactorItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "PreFactorItems",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
