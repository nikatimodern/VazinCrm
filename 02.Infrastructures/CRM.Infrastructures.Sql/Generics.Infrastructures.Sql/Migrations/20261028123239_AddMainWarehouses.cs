using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations
{
    /// <inheritdoc />
    public partial class AddMainWarehouses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMain",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            foreach (var id in new[] {
                new Guid("a732c1a0-8710-4b2f-9501-000000000001"),
                new Guid("a732c1a0-8710-4b2f-9501-000000000002"),
                new Guid("a732c1a0-8710-4b2f-9501-000000000003")
            }) {
                migrationBuilder.UpdateData(
                    table: "Warehouses", keyColumn: "Id", keyValue: id,
                    column: "IsMain", value: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMain",
                table: "Warehouses");
        }
    }
}
