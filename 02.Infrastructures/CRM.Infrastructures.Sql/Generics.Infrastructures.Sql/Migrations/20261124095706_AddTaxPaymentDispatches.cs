using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxPaymentDispatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxPaymentDispatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceDispatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountancyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    ReferenceHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProtectedRequest = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProtectedResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttemptStartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxPaymentDispatches", x => x.Id);
                    table.CheckConstraint("CK_TaxPaymentDispatches_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_TaxPaymentDispatches_State", "([Status] = 0 AND [AttemptId] IS NULL AND [AttemptStartedAt] IS NULL AND [ResolvedAt] IS NULL) OR ([Status] IN (1, 2) AND [AttemptId] IS NOT NULL AND [AttemptStartedAt] IS NOT NULL AND [ResolvedAt] IS NULL) OR ([Status] IN (3, 4) AND [AttemptId] IS NOT NULL AND [AttemptStartedAt] IS NOT NULL AND [ResolvedAt] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_TaxPaymentDispatches_TaxDispatches_InvoiceDispatchId",
                        column: x => x.InvoiceDispatchId,
                        principalTable: "TaxDispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxPaymentDispatches_AccountancyId",
                table: "TaxPaymentDispatches",
                column: "AccountancyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxPaymentDispatches_InvoiceDispatchId_ReferenceHash",
                table: "TaxPaymentDispatches",
                columns: new[] { "InvoiceDispatchId", "ReferenceHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxPaymentDispatches");
        }
    }
}
