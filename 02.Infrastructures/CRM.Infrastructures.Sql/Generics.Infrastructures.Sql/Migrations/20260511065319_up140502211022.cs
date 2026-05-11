using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140502211022 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_RequestAmountOrderHeads_OrderHeads_OrderHeadId",
                table: "RequestAmountOrderHeads",
                column: "OrderHeadId",
                principalTable: "OrderHeads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestAmountOrderHeads_OrderHeads_OrderHeadId",
                table: "RequestAmountOrderHeads");
        }
    }
}
