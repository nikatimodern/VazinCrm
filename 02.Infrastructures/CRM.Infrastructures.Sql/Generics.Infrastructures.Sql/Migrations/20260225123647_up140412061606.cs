using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140412061606 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cashes_Cashiers_CashierId",
                table: "Cashes");

            migrationBuilder.DropIndex(
                name: "IX_Cashes_CashierId",
                table: "Cashes");

            migrationBuilder.DropColumn(
                name: "CashierId",
                table: "Cashes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CashierId",
                table: "Cashes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Cashes_CashierId",
                table: "Cashes",
                column: "CashierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cashes_Cashiers_CashierId",
                table: "Cashes",
                column: "CashierId",
                principalTable: "Cashiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
