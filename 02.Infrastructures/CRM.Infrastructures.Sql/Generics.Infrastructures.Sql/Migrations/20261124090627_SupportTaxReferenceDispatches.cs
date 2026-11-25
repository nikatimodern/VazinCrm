using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class SupportTaxReferenceDispatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TaxDispatches_Reference",
                table: "TaxDispatches");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDispatches_FactorId_Kind",
                table: "TaxDispatches",
                columns: new[] { "FactorId", "Kind" },
                unique: true,
                filter: "[Kind] IN (1, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDispatches_OneCancellation",
                table: "TaxDispatches",
                column: "ReferencedDispatchId",
                unique: true,
                filter: "[Kind] = 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaxDispatches_Reference",
                table: "TaxDispatches",
                sql: "([Kind] = 0 AND [ReferencedDispatchId] IS NULL) OR ([Kind] IN (1, 2, 3) AND [ReferencedDispatchId] IS NOT NULL AND [ReferencedDispatchId] <> [Id])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxDispatches_FactorId_Kind",
                table: "TaxDispatches");

            migrationBuilder.DropIndex(
                name: "IX_TaxDispatches_OneCancellation",
                table: "TaxDispatches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaxDispatches_Reference",
                table: "TaxDispatches");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaxDispatches_Reference",
                table: "TaxDispatches",
                sql: "([Kind] = 0 AND [ReferencedDispatchId] IS NULL) OR ([Kind] IN (1, 2) AND [ReferencedDispatchId] IS NOT NULL AND [ReferencedDispatchId] <> [Id])");
        }
    }
}
