using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140502151236 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_RequestAmounts_RequestAmountId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_RequestAmountId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RequestAmountId",
                table: "Payments");

            migrationBuilder.AddColumn<int>(
                name: "paymentMode",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RequestAmountPayments",
                columns: table => new
                {
                    RequestAmountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAmountPayments", x => new { x.RequestAmountId, x.PaymentId });
                    table.ForeignKey(
                        name: "FK_RequestAmountPayments_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestAmountPayments_RequestAmounts_RequestAmountId",
                        column: x => x.RequestAmountId,
                        principalTable: "RequestAmounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestAmountPayments_PaymentId",
                table: "RequestAmountPayments",
                column: "PaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestAmountPayments");

            migrationBuilder.DropColumn(
                name: "paymentMode",
                table: "Payments");

            migrationBuilder.AddColumn<Guid>(
                name: "RequestAmountId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RequestAmountId",
                table: "Payments",
                column: "RequestAmountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_RequestAmounts_RequestAmountId",
                table: "Payments",
                column: "RequestAmountId",
                principalTable: "RequestAmounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
