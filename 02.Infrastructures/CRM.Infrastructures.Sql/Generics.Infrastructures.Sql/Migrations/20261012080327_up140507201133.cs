using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507201133 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DatabaseIp",
                table: "ExternalSoftwares",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatabaseName",
                table: "ExternalSoftwares",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatabasePassword",
                table: "ExternalSoftwares",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatabaseUserName",
                table: "ExternalSoftwares",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoftwareName",
                table: "ExternalSoftwares",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SoftwareNameTxt",
                table: "ExternalSoftwares",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoftwareTypeTxt",
                table: "ExternalSoftwares",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DatabaseIp",
                table: "ExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "DatabaseName",
                table: "ExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "DatabasePassword",
                table: "ExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "DatabaseUserName",
                table: "ExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "SoftwareName",
                table: "ExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "SoftwareNameTxt",
                table: "ExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "SoftwareTypeTxt",
                table: "ExternalSoftwares");
        }
    }
}
