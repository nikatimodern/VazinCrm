using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140506121253 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WarehouseUsers",
                table: "WarehouseUsers");
            
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "WarehouseUsers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
            
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
            
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "WarehouseUsers",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
            
            migrationBuilder.AddPrimaryKey(
                name: "PK_WarehouseUsers",
                table: "WarehouseUsers",
                columns: new[] { "UserId", "WarehouseId" });
        }
    }
}
