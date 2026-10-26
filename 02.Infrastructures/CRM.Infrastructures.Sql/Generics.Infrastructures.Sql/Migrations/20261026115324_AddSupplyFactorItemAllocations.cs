using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations
{
    /// <inheritdoc />
    public partial class AddSupplyFactorItemAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplyFactorItemAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplyFactorItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FactorItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyFactorItemAllocations", x => x.Id);
                    table.CheckConstraint("CK_SupplyFactorItemAllocations_Weight", "[Weight] > 0");
                    table.ForeignKey(
                        name: "FK_SupplyFactorItemAllocations_FactorItems_FactorItemId",
                        column: x => x.FactorItemId,
                        principalTable: "FactorItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyFactorItemAllocations_SupplyFactorItems_SupplyFactorItemId",
                        column: x => x.SupplyFactorItemId,
                        principalTable: "SupplyFactorItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });


            migrationBuilder.CreateIndex(
                name: "IX_SupplyFactorItemAllocations_FactorItemId",
                table: "SupplyFactorItemAllocations",
                column: "FactorItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyFactorItemAllocations_SupplyFactorItemId_FactorItemId",
                table: "SupplyFactorItemAllocations",
                columns: new[] { "SupplyFactorItemId", "FactorItemId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SupplyFactorItemAllocations");
        }
    }
}
