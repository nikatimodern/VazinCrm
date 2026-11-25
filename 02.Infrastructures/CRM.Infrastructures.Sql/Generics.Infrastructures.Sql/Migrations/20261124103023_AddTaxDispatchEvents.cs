using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations
{
    /// <inheritdoc />
    public partial class AddTaxDispatchEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxDispatchEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxDispatchEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxDispatchEvents_TaxDispatches_DispatchId",
                        column: x => x.DispatchId,
                        principalTable: "TaxDispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDispatchEvents_DispatchId_Id",
                table: "TaxDispatchEvents",
                columns: new[] { "DispatchId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxDispatchEvents");
        }
    }
}
