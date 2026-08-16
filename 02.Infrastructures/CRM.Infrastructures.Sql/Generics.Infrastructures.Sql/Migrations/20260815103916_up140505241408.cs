using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140505241408 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Factors_DealId",
                table: "Factors",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_Accountancies_DealId",
                table: "Accountancies",
                column: "DealId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accountancies_Deals_DealId",
                table: "Accountancies",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Factors_Deals_DealId",
                table: "Factors",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accountancies_Deals_DealId",
                table: "Accountancies");

            migrationBuilder.DropForeignKey(
                name: "FK_Factors_Deals_DealId",
                table: "Factors");

            migrationBuilder.DropIndex(
                name: "IX_Factors_DealId",
                table: "Factors");

            migrationBuilder.DropIndex(
                name: "IX_Accountancies_DealId",
                table: "Accountancies");
        }
    }
}
