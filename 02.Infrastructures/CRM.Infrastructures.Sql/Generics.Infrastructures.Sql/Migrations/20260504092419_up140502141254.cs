using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140502141254 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ToAccount",
                table: "Payments",
                newName: "ToAccountTxt");

            migrationBuilder.RenameColumn(
                name: "FromAccount",
                table: "Payments",
                newName: "FromAccountTxt");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentDate",
                table: "Payments",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<Guid>(
                name: "FromAccountId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RequestAmountId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ToAccountId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RequestAmountId",
                table: "Payments",
                column: "RequestAmountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_RequestAmounts_RequestAmountId",
                table: "Payments",
                column: "RequestAmountId",
                principalTable: "RequestAmounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_RequestAmounts_RequestAmountId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_RequestAmountId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FromAccountId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RequestAmountId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ToAccountId",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "ToAccountTxt",
                table: "Payments",
                newName: "ToAccount");

            migrationBuilder.RenameColumn(
                name: "FromAccountTxt",
                table: "Payments",
                newName: "FromAccount");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentDate",
                table: "Payments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
