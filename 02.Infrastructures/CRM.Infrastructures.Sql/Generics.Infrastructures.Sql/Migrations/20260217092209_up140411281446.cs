using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140411281446 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplyFactorId",
                table: "Payments");

            migrationBuilder.AddColumn<byte>(
                name: "OfficialType",
                table: "SupplyFactors",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfficialType",
                table: "SupplyFactors");

            migrationBuilder.AddColumn<Guid>(
                name: "SupplyFactorId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
