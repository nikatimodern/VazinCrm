using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CRM.Infrastructures.Sql.Migrations
{
    public partial class up14031213 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "Carizos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "Carizos",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Carizos");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "Carizos");
        }
    }
}
