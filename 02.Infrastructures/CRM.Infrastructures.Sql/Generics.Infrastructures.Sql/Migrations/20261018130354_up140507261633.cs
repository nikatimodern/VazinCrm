using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507261633 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExternalSoftwareSetting_ExternalSoftwares_ExternalSoftwareId",
                table: "ExternalSoftwareSetting");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExternalSoftwareSetting",
                table: "ExternalSoftwareSetting");

            migrationBuilder.RenameTable(
                name: "ExternalSoftwareSetting",
                newName: "ExternalSoftwareSettings");

            migrationBuilder.RenameIndex(
                name: "IX_ExternalSoftwareSetting_ExternalSoftwareId",
                table: "ExternalSoftwareSettings",
                newName: "IX_ExternalSoftwareSettings_ExternalSoftwareId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExternalSoftwareSettings",
                table: "ExternalSoftwareSettings",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalSoftwareSettings_ExternalSoftwares_ExternalSoftwareId",
                table: "ExternalSoftwareSettings",
                column: "ExternalSoftwareId",
                principalTable: "ExternalSoftwares",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExternalSoftwareSettings_ExternalSoftwares_ExternalSoftwareId",
                table: "ExternalSoftwareSettings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExternalSoftwareSettings",
                table: "ExternalSoftwareSettings");

            migrationBuilder.RenameTable(
                name: "ExternalSoftwareSettings",
                newName: "ExternalSoftwareSetting");

            migrationBuilder.RenameIndex(
                name: "IX_ExternalSoftwareSettings_ExternalSoftwareId",
                table: "ExternalSoftwareSetting",
                newName: "IX_ExternalSoftwareSetting_ExternalSoftwareId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExternalSoftwareSetting",
                table: "ExternalSoftwareSetting",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalSoftwareSetting_ExternalSoftwares_ExternalSoftwareId",
                table: "ExternalSoftwareSetting",
                column: "ExternalSoftwareId",
                principalTable: "ExternalSoftwares",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
