using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140506010905 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DealId",
                table: "SupplyFactors",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyFactors_DealId",
                table: "SupplyFactors",
                column: "DealId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyFactors_Deals_DealId",
                table: "SupplyFactors",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplyFactors_Deals_DealId",
                table: "SupplyFactors");

            migrationBuilder.DropIndex(
                name: "IX_SupplyFactors_DealId",
                table: "SupplyFactors");

            migrationBuilder.DropColumn(
                name: "DealId",
                table: "SupplyFactors");
        }
    }
}
