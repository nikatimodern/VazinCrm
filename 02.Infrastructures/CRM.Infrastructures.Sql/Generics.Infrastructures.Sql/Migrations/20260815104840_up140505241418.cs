using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140505241418 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reports_DealId",
                table: "Reports",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_PreFactors_DealId",
                table: "PreFactors",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHeads_DealId",
                table: "OrderHeads",
                column: "DealId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderHeads_Deals_DealId",
                table: "OrderHeads",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PreFactors_Deals_DealId",
                table: "PreFactors",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Deals_DealId",
                table: "Reports",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderHeads_Deals_DealId",
                table: "OrderHeads");

            migrationBuilder.DropForeignKey(
                name: "FK_PreFactors_Deals_DealId",
                table: "PreFactors");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Deals_DealId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_DealId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_PreFactors_DealId",
                table: "PreFactors");

            migrationBuilder.DropIndex(
                name: "IX_OrderHeads_DealId",
                table: "OrderHeads");
        }
    }
}
