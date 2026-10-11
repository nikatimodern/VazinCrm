using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507191308 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BankAccountExternalSoftwares_ExSoftwareId",
                table: "BankAccountExternalSoftwares",
                column: "ExSoftwareId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankAccountExternalSoftwares_ExternalSoftwares_ExSoftwareId",
                table: "BankAccountExternalSoftwares",
                column: "ExSoftwareId",
                principalTable: "ExternalSoftwares",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankAccountExternalSoftwares_ExternalSoftwares_ExSoftwareId",
                table: "BankAccountExternalSoftwares");

            migrationBuilder.DropIndex(
                name: "IX_BankAccountExternalSoftwares_ExSoftwareId",
                table: "BankAccountExternalSoftwares");
        }
    }
}
