using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507211349 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashExternalSoftwares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalSoftwareId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TafsiliCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashExternalSoftwares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashExternalSoftwares_Cashes_CashId",
                        column: x => x.CashId,
                        principalTable: "Cashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CashExternalSoftwares_ExternalSoftwares_ExternalSoftwareId",
                        column: x => x.ExternalSoftwareId,
                        principalTable: "ExternalSoftwares",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashExternalSoftwares_CashId",
                table: "CashExternalSoftwares",
                column: "CashId");

            migrationBuilder.CreateIndex(
                name: "IX_CashExternalSoftwares_ExternalSoftwareId",
                table: "CashExternalSoftwares",
                column: "ExternalSoftwareId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashExternalSoftwares");
        }
    }
}
