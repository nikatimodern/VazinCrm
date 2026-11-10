using System;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations;

[DbContext(typeof(CRMContextDb))]
[Migration("20261110200000_AddAccessRolePermissions")]
public partial class AddAccessRolePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AccessRolePermissions",
            columns: table => new
            {
                RoleId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                AccessPermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccessRolePermissions", x => new { x.RoleId, x.AccessPermissionId });
                table.ForeignKey(name: "FK_AccessRolePermissions_AccessPermissions_AccessPermissionId",
                    column: x => x.AccessPermissionId, principalTable: "AccessPermissions",
                    principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_AccessRolePermissions_AccessPermissionId",
            table: "AccessRolePermissions", column: "AccessPermissionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AccessRolePermissions");
    }
}
