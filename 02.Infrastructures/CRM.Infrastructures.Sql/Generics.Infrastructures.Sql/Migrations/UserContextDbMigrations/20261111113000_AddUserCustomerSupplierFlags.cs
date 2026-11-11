using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations;

[DbContext(typeof(UserContextDb))]
[Migration("20261111113000_AddUserCustomerSupplierFlags")]
public partial class AddUserCustomerSupplierFlags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsCustomer",
            table: "AspNetUsers",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsSupplier",
            table: "AspNetUsers",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsCustomer", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "IsSupplier", table: "AspNetUsers");
    }
}
