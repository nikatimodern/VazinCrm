using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxDispatchQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxDispatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FactorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    ReferencedDispatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    MemoryTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MemoryUpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MemoryWasActive = table.Column<bool>(type: "bit", nullable: false),
                    QueuedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxDispatches", x => x.Id);
                    table.CheckConstraint("CK_TaxDispatches_Reference", "([Kind] = 0 AND [ReferencedDispatchId] IS NULL) OR ([Kind] IN (1, 2) AND [ReferencedDispatchId] IS NOT NULL AND [ReferencedDispatchId] <> [Id])");
                    table.CheckConstraint("CK_TaxDispatches_State", "([Status] = 0 AND [AttemptId] IS NULL AND [LeaseUntilUtc] IS NULL) OR ([Status] = 1 AND [AttemptId] IS NOT NULL AND [LeaseUntilUtc] IS NOT NULL) OR ([Status] IN (2, 3, 4) AND [AttemptId] IS NOT NULL AND [LeaseUntilUtc] IS NULL)");
                    table.ForeignKey(
                        name: "FK_TaxDispatches_TaxDispatches_ReferencedDispatchId",
                        column: x => x.ReferencedDispatchId,
                        principalTable: "TaxDispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxDispatches_TaxInvoiceSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "TaxInvoiceSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDispatches_FactorId",
                table: "TaxDispatches",
                column: "FactorId",
                unique: true,
                filter: "[Kind] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDispatches_ReferencedDispatchId",
                table: "TaxDispatches",
                column: "ReferencedDispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDispatches_SnapshotId",
                table: "TaxDispatches",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxDispatches_Status_QueuedUtc",
                table: "TaxDispatches",
                columns: new[] { "Status", "QueuedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxDispatches");
        }
    }
}
