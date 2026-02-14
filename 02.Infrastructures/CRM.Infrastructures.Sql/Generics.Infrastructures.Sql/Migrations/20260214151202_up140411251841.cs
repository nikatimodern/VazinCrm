using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140411251841 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_Contracts_contractId",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_contractId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "FactorFileUrl",
                table: "SupplyFactors");

            migrationBuilder.DropColumn(
                name: "contractId",
                table: "Attachments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FactorFileUrl",
                table: "SupplyFactors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "contractId",
                table: "Attachments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_contractId",
                table: "Attachments",
                column: "contractId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Contracts_contractId",
                table: "Attachments",
                column: "contractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
