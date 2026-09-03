using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140506121128 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WarehouseUsers",
                table: "WarehouseUsers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "WarehouseUsers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WarehouseUsers",
                table: "WarehouseUsers",
                columns: new[] { "UserId", "WarehouseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WarehouseUsers",
                table: "WarehouseUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "WarehouseUsers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_WarehouseUsers",
                table: "WarehouseUsers",
                column: "Id");
        }
    }
}
