using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507261622 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccountExternalSoftwares");

            migrationBuilder.CreateTable(
                name: "ExternalSoftwareSetting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalSoftwareId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettingName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SettingNameCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalSoftwareSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalSoftwareSetting_ExternalSoftwares_ExternalSoftwareId",
                        column: x => x.ExternalSoftwareId,
                        principalTable: "ExternalSoftwares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSoftwareSetting_ExternalSoftwareId",
                table: "ExternalSoftwareSetting",
                column: "ExternalSoftwareId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalSoftwareSetting");

            migrationBuilder.CreateTable(
                name: "BankAccountExternalSoftwares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExSoftwareId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExSoftwareName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountExternalSoftwares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccountExternalSoftwares_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BankAccountExternalSoftwares_ExternalSoftwares_ExSoftwareId",
                        column: x => x.ExSoftwareId,
                        principalTable: "ExternalSoftwares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountExternalSoftwares_BankAccountId",
                table: "BankAccountExternalSoftwares",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountExternalSoftwares_ExSoftwareId",
                table: "BankAccountExternalSoftwares",
                column: "ExSoftwareId");
        }
    }
}
