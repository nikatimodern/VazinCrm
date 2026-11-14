using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    [DbContext(typeof(CRMContextDb))]
    [Migration("20261114103000_AddInvalidNumberQualityControlOutcome")]
    public partial class AddInvalidNumberQualityControlOutcome : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_QualityControls_Status",
                table: "QualityControls");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QualityControls_Status",
                table: "QualityControls",
                sql: "[Status] IN (1, 2, 4, 5)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_QualityControls_Status",
                table: "QualityControls");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QualityControls_Status",
                table: "QualityControls",
                sql: "[Status] IN (1, 2, 4)");
        }
    }
}
