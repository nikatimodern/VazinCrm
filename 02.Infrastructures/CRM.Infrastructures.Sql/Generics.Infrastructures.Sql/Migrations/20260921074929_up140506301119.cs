using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140506301119 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "PreFactorItems",
                type: "decimal(20,0)",
                precision: 20,
                scale: 0,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "FactorItems",
                type: "decimal(20,0)",
                precision: 20,
                scale: 0,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "TaxAfterDiscount",
                table: "ASettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "PreFactorItems");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "FactorItems");

            migrationBuilder.DropColumn(
                name: "TaxAfterDiscount",
                table: "ASettings");
        }
    }
}
