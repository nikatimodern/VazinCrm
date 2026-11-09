using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    [DbContext(typeof(CRMContextDb))]
    [Migration("20261109121500_AddQualityControlNoResponse")]
    public partial class AddQualityControlNoResponse : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "NoResponse", table: "QualityControls",
                type: "bit", nullable: false, defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "NoResponse", table: "QualityControls");
        }
    }
