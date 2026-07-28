using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140504301515 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BankAccountId",
                table: "Cashes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountTxt",
                table: "Cashes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentVoucherNumber",
                table: "Cashes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "Cashes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccountCode",
                table: "BankAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cashes_BankAccountId",
                table: "Cashes",
                column: "BankAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cashes_BankAccounts_BankAccountId",
                table: "Cashes",
                column: "BankAccountId",
                principalTable: "BankAccounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cashes_BankAccounts_BankAccountId",
                table: "Cashes");

            migrationBuilder.DropIndex(
                name: "IX_Cashes_BankAccountId",
                table: "Cashes");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                table: "Cashes");

            migrationBuilder.DropColumn(
                name: "BankAccountTxt",
                table: "Cashes");

            migrationBuilder.DropColumn(
                name: "PaymentVoucherNumber",
                table: "Cashes");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "Cashes");

            migrationBuilder.DropColumn(
                name: "AccountCode",
                table: "BankAccounts");
        }
    }
}
