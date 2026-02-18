using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140411261320 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DocTypeTXT",
                table: "RequestAmounts",
                newName: "DocTypeTxt");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "SupplyFactors",
                type: "decimal(20,0)",
                precision: 20,
                scale: 0,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "DocType",
                table: "RequestAmounts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "RequestAmounts",
                type: "decimal(20,0)",
                precision: 20,
                scale: 0,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AcceptedAmount",
                table: "RequestAmounts",
                type: "decimal(20,0)",
                precision: 20,
                scale: 0,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RequestStatus",
                table: "RequestAmounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RequestStatusTxt",
                table: "RequestAmounts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedAmount",
                table: "RequestAmounts");

            migrationBuilder.DropColumn(
                name: "RequestStatus",
                table: "RequestAmounts");

            migrationBuilder.DropColumn(
                name: "RequestStatusTxt",
                table: "RequestAmounts");

            migrationBuilder.RenameColumn(
                name: "DocTypeTxt",
                table: "RequestAmounts",
                newName: "DocTypeTXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "SupplyFactors",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldPrecision: 20,
                oldScale: 0);

            migrationBuilder.AlterColumn<int>(
                name: "DocType",
                table: "RequestAmounts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Amount",
                table: "RequestAmounts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldPrecision: 20,
                oldScale: 0);
        }
    }
}
