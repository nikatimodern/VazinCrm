using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507191259 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "BankAccounts");

            migrationBuilder.RenameColumn(
                name: "SoftwareName",
                table: "BankAccountExternalSoftwares",
                newName: "ExSoftwareName");

            migrationBuilder.AddColumn<Guid>(
                name: "ExSoftwareId",
                table: "BankAccountExternalSoftwares",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExSoftwareId",
                table: "BankAccountExternalSoftwares");

            migrationBuilder.RenameColumn(
                name: "ExSoftwareName",
                table: "BankAccountExternalSoftwares",
                newName: "SoftwareName");

            migrationBuilder.AddColumn<bool>(
                name: "Status",
                table: "BankAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
