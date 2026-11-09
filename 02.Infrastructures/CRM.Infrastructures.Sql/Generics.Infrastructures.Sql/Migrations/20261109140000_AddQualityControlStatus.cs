using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    // Schema-only migration. Existing rows stay NULL until the separately planned backfill.
    [DbContext(typeof(CRMContextDb))]
    [Migration("20261109140000_AddQualityControlStatus")]
    public partial class AddQualityControlStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "QualityControls",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_QualityControls_Status",
                table: "QualityControls",
                sql: "[Status] IN (1, 2, 4)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_QualityControls_Status",
                table: "QualityControls");
            migrationBuilder.DropColumn(name: "Status", table: "QualityControls");
        }
    }
}
