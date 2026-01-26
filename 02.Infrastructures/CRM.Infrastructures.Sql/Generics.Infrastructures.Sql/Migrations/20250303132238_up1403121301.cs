using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CRM.Infrastructures.Sql.Migrations
{
    public partial class up1403121301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReciveDate",
                table: "Accountancies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReciveDateM",
                table: "Accountancies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VosolDate",
                table: "Accountancies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VosolDateM",
                table: "Accountancies",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReciveDate",
                table: "Accountancies");

            migrationBuilder.DropColumn(
                name: "ReciveDateM",
                table: "Accountancies");

            migrationBuilder.DropColumn(
                name: "VosolDate",
                table: "Accountancies");

            migrationBuilder.DropColumn(
                name: "VosolDateM",
                table: "Accountancies");
        }
    }
}
