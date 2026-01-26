using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up1404043001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ToAccount",
                table: "Accountancies",
                newName: "BankAccountTxt");

            migrationBuilder.AddColumn<Guid>(
                name: "PreFactorItemId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BankAccountId",
                table: "Accountancies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accountancies_BankAccountId",
                table: "Accountancies",
                column: "BankAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accountancies_BankAccounts_BankAccountId",
                table: "Accountancies",
                column: "BankAccountId",
                principalTable: "BankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accountancies_BankAccounts_BankAccountId",
                table: "Accountancies");

            migrationBuilder.DropIndex(
                name: "IX_Accountancies_BankAccountId",
                table: "Accountancies");

            migrationBuilder.DropColumn(
                name: "PreFactorItemId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                table: "Accountancies");

            migrationBuilder.RenameColumn(
                name: "BankAccountTxt",
                table: "Accountancies",
                newName: "ToAccount");
        }
    }
}
