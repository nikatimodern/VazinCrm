using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up1405072016525 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashierExternalSoftwareMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalSoftwareId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TafsiliCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashierExternalSoftwareMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashierExternalSoftwareMappings_Cashiers_CashierId",
                        column: x => x.CashierId,
                        principalTable: "Cashiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CashierExternalSoftwareMappings_ExternalSoftwares_ExternalSoftwareId",
                        column: x => x.ExternalSoftwareId,
                        principalTable: "ExternalSoftwares",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashierExternalSoftwareMappings_CashierId",
                table: "CashierExternalSoftwareMappings",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_CashierExternalSoftwareMappings_ExternalSoftwareId",
                table: "CashierExternalSoftwareMappings",
                column: "ExternalSoftwareId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashierExternalSoftwareMappings");
        }
    }
}
