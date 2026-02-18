using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140411271735 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentSupplyFactors_Payments_PaymentID",
                table: "PaymentSupplyFactors");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentSupplyFactors_SupplyFactors_SupplyFactorID",
                table: "PaymentSupplyFactors");

            migrationBuilder.RenameColumn(
                name: "SupplyFactorID",
                table: "PaymentSupplyFactors",
                newName: "SupplyFactorId");

            migrationBuilder.RenameColumn(
                name: "PaymentID",
                table: "PaymentSupplyFactors",
                newName: "PaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentSupplyFactors_SupplyFactorID",
                table: "PaymentSupplyFactors",
                newName: "IX_PaymentSupplyFactors_SupplyFactorId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Payments",
                type: "decimal(20,0)",
                precision: 20,
                scale: 0,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "Payments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PaymentDateTxt",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTypeTxt",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentSupplyFactors_Payments_PaymentId",
                table: "PaymentSupplyFactors",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentSupplyFactors_SupplyFactors_SupplyFactorId",
                table: "PaymentSupplyFactors",
                column: "SupplyFactorId",
                principalTable: "SupplyFactors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentSupplyFactors_Payments_PaymentId",
                table: "PaymentSupplyFactors");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentSupplyFactors_SupplyFactors_SupplyFactorId",
                table: "PaymentSupplyFactors");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentDateTxt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentTypeTxt",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "SupplyFactorId",
                table: "PaymentSupplyFactors",
                newName: "SupplyFactorID");

            migrationBuilder.RenameColumn(
                name: "PaymentId",
                table: "PaymentSupplyFactors",
                newName: "PaymentID");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentSupplyFactors_SupplyFactorId",
                table: "PaymentSupplyFactors",
                newName: "IX_PaymentSupplyFactors_SupplyFactorID");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldPrecision: 20,
                oldScale: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentSupplyFactors_Payments_PaymentID",
                table: "PaymentSupplyFactors",
                column: "PaymentID",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentSupplyFactors_SupplyFactors_SupplyFactorID",
                table: "PaymentSupplyFactors",
                column: "SupplyFactorID",
                principalTable: "SupplyFactors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
