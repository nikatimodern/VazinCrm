using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations;

[DbContext(typeof(CRMContextDb))]
[Migration("20261112090000_RemoveBankAccountCustomerCode")]
public partial class RemoveBankAccountCustomerCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Validate existing CustomerId values before making a destructive change.
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1
                FROM dbo.BankAccounts AS b
                WHERE b.CustomerId IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM dbo.Customers AS c
                      WHERE c.Id = b.CustomerId
                  )
            )
            BEGIN
                ;THROW 50001, 'BankAccounts contains CustomerId values without a matching Customer. Resolve these values before applying this migration.', 1;
            END
            """);

        migrationBuilder.CreateIndex(
            name: "IX_BankAccounts_CustomerId",
            table: "BankAccounts",
            column: "CustomerId");

        migrationBuilder.AddForeignKey(
            name: "FK_BankAccounts_Customers_CustomerId",
            table: "BankAccounts",
            column: "CustomerId",
            principalTable: "Customers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.DropColumn(
            name: "CustomerCode",
            table: "BankAccounts");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_BankAccounts_Customers_CustomerId",
            table: "BankAccounts");

        migrationBuilder.DropIndex(
            name: "IX_BankAccounts_CustomerId",
            table: "BankAccounts");

        migrationBuilder.AddColumn<int>(
            name: "CustomerCode",
            table: "BankAccounts",
            type: "int",
            nullable: true);
    }
}
