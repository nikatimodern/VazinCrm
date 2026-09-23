using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507011710 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FactorDate",
                table: "SupplyFactors",
                newName: "DateMiladi");

            migrationBuilder.RenameColumn(
                name: "FactorDateTxt",
                table: "SupplyFactors",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "FactorSerial",
                table: "SupplyFactors",
                newName: "SerialNumber");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "SupplyFactors",
                newName: "PaymentAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "SumTotalAmount",
                table: "SupplyFactors",
                type: "decimal(20,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "SupplyFactors",
                type: "decimal(20,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTax",
                table: "SupplyFactors",
                type: "decimal(20,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SupplyFactors",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SumTotalAmount",
                table: "SupplyFactors");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "SupplyFactors");

            migrationBuilder.DropColumn(
                name: "TotalTax",
                table: "SupplyFactors");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "SupplyFactors");

            migrationBuilder.RenameColumn(
                name: "DateMiladi",
                table: "SupplyFactors",
                newName: "FactorDate");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "SupplyFactors",
                newName: "FactorDateTxt");

            migrationBuilder.RenameColumn(
                name: "SerialNumber",
                table: "SupplyFactors",
                newName: "FactorSerial");

            migrationBuilder.RenameColumn(
                name: "PaymentAmount",
                table: "SupplyFactors",
                newName: "Amount");
        }
    }
}
