using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up14040617 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DealSerialNumber",
                table: "PreFactors");

            migrationBuilder.AddColumn<Guid>(
                name: "DealId",
                table: "Reports",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DealSerialNumber",
                table: "Reports",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DealId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "DealSerialNumber",
                table: "Reports");

            migrationBuilder.AddColumn<Guid>(
                name: "DealSerialNumber",
                table: "PreFactors",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
