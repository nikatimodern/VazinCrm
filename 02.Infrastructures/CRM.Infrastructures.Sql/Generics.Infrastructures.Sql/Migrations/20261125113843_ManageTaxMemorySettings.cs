using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class ManageTaxMemorySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxMemories_FiscalId",
                table: "TaxMemories");

            migrationBuilder.AddColumn<string>(
                name: "CredentialFingerprint",
                table: "TaxMemories",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Environment",
                table: "TaxMemories",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Protocol",
                table: "TaxMemories",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<bool>(
                name: "SandboxEnabled",
                table: "TaxMemories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SellerEconomicCode",
                table: "TaxMemories",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaxMemoryAuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OccurredUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxMemoryAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxMemoryAuditEvents_TaxMemories_MemoryId",
                        column: x => x.MemoryId,
                        principalTable: "TaxMemories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxMemories_Environment_FiscalId",
                table: "TaxMemories",
                columns: new[] { "Environment", "FiscalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxMemoryAuditEvents_MemoryId",
                table: "TaxMemoryAuditEvents",
                column: "MemoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxMemoryAuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_TaxMemories_Environment_FiscalId",
                table: "TaxMemories");

            migrationBuilder.DropColumn(
                name: "CredentialFingerprint",
                table: "TaxMemories");

            migrationBuilder.DropColumn(
                name: "Environment",
                table: "TaxMemories");

            migrationBuilder.DropColumn(
                name: "Protocol",
                table: "TaxMemories");

            migrationBuilder.DropColumn(
                name: "SandboxEnabled",
                table: "TaxMemories");

            migrationBuilder.DropColumn(
                name: "SellerEconomicCode",
                table: "TaxMemories");

            migrationBuilder.CreateIndex(
                name: "IX_TaxMemories_FiscalId",
                table: "TaxMemories",
                column: "FiscalId",
                unique: true);
        }
    }
}
