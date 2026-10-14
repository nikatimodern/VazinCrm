using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507201357 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoftwareName",
                table: "ExternalSoftwares");

            migrationBuilder.RenameColumn(
                name: "SoftwareNameTxt",
                table: "ExternalSoftwares",
                newName: "SoftwareBaseItemTxt");

            migrationBuilder.AddColumn<Guid>(
                name: "SoftwareBaseItemId",
                table: "ExternalSoftwares",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoftwareBaseItemId",
                table: "ExternalSoftwares");

            migrationBuilder.RenameColumn(
                name: "SoftwareBaseItemTxt",
                table: "ExternalSoftwares",
                newName: "SoftwareNameTxt");

            migrationBuilder.AddColumn<int>(
                name: "SoftwareName",
                table: "ExternalSoftwares",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
