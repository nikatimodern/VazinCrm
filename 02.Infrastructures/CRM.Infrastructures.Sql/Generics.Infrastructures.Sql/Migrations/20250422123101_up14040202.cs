using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up14040202 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerCode",
                table: "OrderHeads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderDateMildi",
                table: "OrderHeads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OrderHeads",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerCode",
                table: "OrderHeads");

            migrationBuilder.DropColumn(
                name: "OrderDateMildi",
                table: "OrderHeads");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrderHeads");
        }
    }
}
