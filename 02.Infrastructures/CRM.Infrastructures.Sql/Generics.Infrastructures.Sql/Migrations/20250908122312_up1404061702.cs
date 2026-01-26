using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up1404061702 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Deals_CustomerId",
                table: "Deals",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deals_Customers_CustomerId",
                table: "Deals",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deals_Customers_CustomerId",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Deals_CustomerId",
                table: "Deals");
        }
    }
}
