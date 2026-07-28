using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140505011111 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Cashes_CostCenterId",
                table: "Cashes",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Cashes_CostTitleId",
                table: "Cashes",
                column: "CostTitleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cashes_BaseCashes_CostTitleId",
                table: "Cashes",
                column: "CostTitleId",
                principalTable: "BaseCashes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cashes_BaseCenterCashes_CostCenterId",
                table: "Cashes",
                column: "CostCenterId",
                principalTable: "BaseCenterCashes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cashes_BaseCashes_CostTitleId",
                table: "Cashes");

            migrationBuilder.DropForeignKey(
                name: "FK_Cashes_BaseCenterCashes_CostCenterId",
                table: "Cashes");

            migrationBuilder.DropIndex(
                name: "IX_Cashes_CostCenterId",
                table: "Cashes");

            migrationBuilder.DropIndex(
                name: "IX_Cashes_CostTitleId",
                table: "Cashes");
        }
    }
}
