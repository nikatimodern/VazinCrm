using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140412031445 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CashierIdTXT",
                table: "Cashes",
                newName: "CashierTxt");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Cashiers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Cashes",
                type: "decimal(20,0)",
                precision: 20,
                scale: 0,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CostDateMiladi",
                table: "Cashes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Cashiers");

            migrationBuilder.DropColumn(
                name: "CostDateMiladi",
                table: "Cashes");

            migrationBuilder.RenameColumn(
                name: "CashierTxt",
                table: "Cashes",
                newName: "CashierIdTXT");

            migrationBuilder.AlterColumn<string>(
                name: "Amount",
                table: "Cashes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldPrecision: 20,
                oldScale: 0);
        }
    }
}
