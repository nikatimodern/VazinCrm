using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507191422 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BankAccountId",
                table: "Cashiers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cashiers_BankAccountId",
                table: "Cashiers",
                column: "BankAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cashiers_BankAccounts_BankAccountId",
                table: "Cashiers",
                column: "BankAccountId",
                principalTable: "BankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cashiers_BankAccounts_BankAccountId",
                table: "Cashiers");

            migrationBuilder.DropIndex(
                name: "IX_Cashiers_BankAccountId",
                table: "Cashiers");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                table: "Cashiers");
        }
    }
}
