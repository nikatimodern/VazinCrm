using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140501311115 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderHeadId",
                table: "RequestAmounts");

            migrationBuilder.CreateTable(
                name: "RequestAmountOrderHeads",
                columns: table => new
                {
                    RequestAmountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderHeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAmountOrderHeads", x => new { x.OrderHeadId, x.RequestAmountId });
                    table.ForeignKey(
                        name: "FK_RequestAmountOrderHeads_RequestAmounts_RequestAmountId",
                        column: x => x.RequestAmountId,
                        principalTable: "RequestAmounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestAmountOrderHeads_RequestAmountId",
                table: "RequestAmountOrderHeads",
                column: "RequestAmountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestAmountOrderHeads");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderHeadId",
                table: "RequestAmounts",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
