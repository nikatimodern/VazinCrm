using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxSubmissionEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TaxDispatches_State",
                table: "TaxDispatches");

            migrationBuilder.AddColumn<int>(
                name: "AuthorityEnvironment",
                table: "TaxDispatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorityProcessingStatus",
                table: "TaxDispatches",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AuthorityReference",
                table: "TaxDispatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateSha256",
                table: "TaxDispatches",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InvoiceSerial",
                table: "TaxDispatches",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectedAcknowledgement",
                table: "TaxDispatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectedProcessingEvidence",
                table: "TaxDispatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectedSubmission",
                table: "TaxDispatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectedWorkFolderEvidence",
                table: "TaxDispatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmissionStartedAt",
                table: "TaxDispatches",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "TaxDispatches",
                type: "nvarchar(22)",
                maxLength: 22,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxDispatches_AuthorityEnvironment_FiscalId_InvoiceSerial",
                table: "TaxDispatches",
                columns: new[] { "AuthorityEnvironment", "FiscalId", "InvoiceSerial" },
                unique: true,
                filter: "[InvoiceSerial] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDispatches_AuthorityEnvironment_TaxId",
                table: "TaxDispatches",
                columns: new[] { "AuthorityEnvironment", "TaxId" },
                unique: true,
                filter: "[TaxId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaxDispatches_State",
                table: "TaxDispatches",
                sql: "([Status] = 0 AND [AttemptId] IS NULL AND [LeaseUntilUtc] IS NULL) OR ([Status] = 1 AND [AttemptId] IS NOT NULL AND [LeaseUntilUtc] IS NOT NULL) OR ([Status] IN (2, 3, 4, 5) AND [AttemptId] IS NOT NULL AND [LeaseUntilUtc] IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxDispatches_AuthorityEnvironment_FiscalId_InvoiceSerial",
                table: "TaxDispatches");

            migrationBuilder.DropIndex(
                name: "IX_TaxDispatches_AuthorityEnvironment_TaxId",
                table: "TaxDispatches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaxDispatches_State",
                table: "TaxDispatches");

            migrationBuilder.DropColumn(
                name: "AuthorityEnvironment",
                table: "TaxDispatches");

            migrationBuilder.DropColumn(
                name: "AuthorityProcessingStatus",
                table: "TaxDispatches");

            migrationBuilder.DropColumn(
                name: "AuthorityReference",
                table: "TaxDispatches");

            migrationBuilder.DropColumn(
                name: "CertificateSha256",
                table: "TaxDispatches");

            migrationBuilder.DropColumn(
                name: "InvoiceSerial",
                table: "TaxDispatches");

            migrationBuilder.DropColumn(
                name: "ProtectedAcknowledgement",
                table: "TaxDispatches");

            migrationBuilder.DropColumn(
                name: "ProtectedProcessingEvidence",
                table: "TaxDispatches");

            migrationBuilder.DropColumn(
                name: "ProtectedSubmission",
                table: "TaxDispatches");

            migrationBuilder.DropColumn(
                name: "ProtectedWorkFolderEvidence",
                table: "TaxDispatches");

            migrationBuilder.DropColumn(
                name: "SubmissionStartedAt",
                table: "TaxDispatches");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "TaxDispatches");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaxDispatches_State",
                table: "TaxDispatches",
                sql: "([Status] = 0 AND [AttemptId] IS NULL AND [LeaseUntilUtc] IS NULL) OR ([Status] = 1 AND [AttemptId] IS NOT NULL AND [LeaseUntilUtc] IS NOT NULL) OR ([Status] IN (2, 3, 4) AND [AttemptId] IS NOT NULL AND [LeaseUntilUtc] IS NULL)");
        }
    }
}
