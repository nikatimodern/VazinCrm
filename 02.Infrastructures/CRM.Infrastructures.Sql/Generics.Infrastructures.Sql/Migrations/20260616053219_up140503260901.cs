using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140503260901 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Checkbooks");

            migrationBuilder.AddColumn<string>(
                name: "SayyadNumber",
                table: "Checks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BankAccountId",
                table: "Checkbooks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Checkbooks_BankAccountId",
                table: "Checkbooks",
                column: "BankAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Checkbooks_BankAccounts_BankAccountId",
                table: "Checkbooks",
                column: "BankAccountId",
                principalTable: "BankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Checkbooks_BankAccounts_BankAccountId",
                table: "Checkbooks");

            migrationBuilder.DropIndex(
                name: "IX_Checkbooks_BankAccountId",
                table: "Checkbooks");

            migrationBuilder.DropColumn(
                name: "SayyadNumber",
                table: "Checks");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                table: "Checkbooks");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Checkbooks",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
