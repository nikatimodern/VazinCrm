using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140503051724 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CashierId",
                table: "Accountancies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestAmountId",
                table: "Accountancies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accountancies_RequestAmountId",
                table: "Accountancies",
                column: "RequestAmountId",
                unique: true,
                filter: "[RequestAmountId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Accountancies_RequestAmounts_RequestAmountId",
                table: "Accountancies",
                column: "RequestAmountId",
                principalTable: "RequestAmounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accountancies_RequestAmounts_RequestAmountId",
                table: "Accountancies");

            migrationBuilder.DropIndex(
                name: "IX_Accountancies_RequestAmountId",
                table: "Accountancies");

            migrationBuilder.DropColumn(
                name: "CashierId",
                table: "Accountancies");

            migrationBuilder.DropColumn(
                name: "RequestAmountId",
                table: "Accountancies");
        }
    }
}
