using System;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations;

[DbContext(typeof(UserContextDb))]
[Migration("20261111100000_AddAccessUserPermissions")]
public partial class AddAccessUserPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AccessUserPermissions",
            columns: table => new
            {
                UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                AccessPermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IsAllowed = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccessUserPermissions", x => new { x.UserId, x.AccessPermissionId });
                table.ForeignKey(
                    name: "FK_AccessUserPermissions_AspNetUsers_UserId",
                    column: x => x.UserId, principalTable: "AspNetUsers",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AccessUserPermissions_AccessPermissions_AccessPermissionId",
                    column: x => x.AccessPermissionId, principalTable: "AccessPermissions",
                    principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_AccessUserPermissions_AccessPermissionId",
            table: "AccessUserPermissions", column: "AccessPermissionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AccessUserPermissions");
    }
}
