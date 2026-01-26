using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up14040815 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CusAcquaintanceWay",
                table: "Deals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CusAcquaintanceWayTxt",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CusAddress",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CusCity",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CusCompanyName",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CusEghtesadiCode",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "CusJuridical",
                table: "Deals",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "CusMelliCode",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CusNumberRegister",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CusPostalCode",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CusProvince",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "CusSex",
                table: "Deals",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<string>(
                name: "CusShenaseMeli",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CusTel",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CusWorkSenf",
                table: "Deals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CusWorkSenfDescription",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CusWorkSenfTxt",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CusAcquaintanceWay",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusAcquaintanceWayTxt",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusAddress",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusCity",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusCompanyName",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusEghtesadiCode",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusJuridical",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusMelliCode",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusNumberRegister",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusPostalCode",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusProvince",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusSex",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusShenaseMeli",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusTel",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusWorkSenf",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusWorkSenfDescription",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CusWorkSenfTxt",
                table: "Deals");
        }
    }
}
