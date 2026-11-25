using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxMemories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxMemories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxProductMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    GoodsId = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    TaxUnit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxProductMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxProductMappings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxInvoiceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FactorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxMemoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProtectedPreview = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxInvoiceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxInvoiceSnapshots_Factors_FactorId",
                        column: x => x.FactorId,
                        principalTable: "Factors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxInvoiceSnapshots_TaxMemories_TaxMemoryId",
                        column: x => x.TaxMemoryId,
                        principalTable: "TaxMemories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxInvoiceSnapshots_FactorId_CreatedUtc",
                table: "TaxInvoiceSnapshots",
                columns: new[] { "FactorId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxInvoiceSnapshots_TaxMemoryId",
                table: "TaxInvoiceSnapshots",
                column: "TaxMemoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxMemories_FiscalId",
                table: "TaxMemories",
                column: "FiscalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxMemories_IsActive",
                table: "TaxMemories",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TaxProductMappings_ProductId_Unit",
                table: "TaxProductMappings",
                columns: new[] { "ProductId", "Unit" },
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TaxProductMappings_ProductId_Unit_GoodsId",
                table: "TaxProductMappings",
                columns: new[] { "ProductId", "Unit", "GoodsId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxInvoiceSnapshots");

            migrationBuilder.DropTable(
                name: "TaxProductMappings");

            migrationBuilder.DropTable(
                name: "TaxMemories");
        }
    }
}
