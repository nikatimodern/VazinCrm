using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140502141124 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestAmountType",
                table: "RequestAmounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RequestAmountTypeTxt",
                table: "RequestAmounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BankAccountType",
                table: "BankAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountTypeTxt",
                table: "BankAccounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerCode",
                table: "BankAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "BankAccounts",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestAmountType",
                table: "RequestAmounts");

            migrationBuilder.DropColumn(
                name: "RequestAmountTypeTxt",
                table: "RequestAmounts");

            migrationBuilder.DropColumn(
                name: "BankAccountType",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "BankAccountTypeTxt",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "CustomerCode",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "BankAccounts");
        }
    }
}
