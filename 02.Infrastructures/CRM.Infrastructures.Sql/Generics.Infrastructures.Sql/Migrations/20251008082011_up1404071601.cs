using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up1404071601 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaterialType",
                table: "Orders",
                newName: "ProductSmallName");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductCode",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductGroup",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductGroupTxt",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProductGroup",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProductGroupTxt",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "ProductSmallName",
                table: "Orders",
                newName: "MaterialType");
        }
    }
}
