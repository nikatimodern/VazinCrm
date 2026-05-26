using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140503051623 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CashId",
                table: "Accountancies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accountancies_CashId",
                table: "Accountancies",
                column: "CashId",
                unique: true,
                filter: "[CashId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Accountancies_Cashes_CashId",
                table: "Accountancies",
                column: "CashId",
                principalTable: "Cashes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accountancies_Cashes_CashId",
                table: "Accountancies");

            migrationBuilder.DropIndex(
                name: "IX_Accountancies_CashId",
                table: "Accountancies");

            migrationBuilder.DropColumn(
                name: "CashId",
                table: "Accountancies");
        }
    }
}
