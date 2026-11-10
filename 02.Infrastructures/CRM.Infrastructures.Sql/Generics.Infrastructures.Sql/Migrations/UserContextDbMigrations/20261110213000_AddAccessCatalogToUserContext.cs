using System;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations;

[DbContext(typeof(UserContextDb))]
[Migration("20261110213000_AddAccessCatalogToUserContext")]
public partial class AddAccessCatalogToUserContext : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AccessEntities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                DbContextName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                ModelFullName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AccessEntities", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AccessPermissions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AccessEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccessPermissions", x => x.Id);
                table.ForeignKey(name: "FK_AccessPermissions_AccessEntities_AccessEntityId",
                    column: x => x.AccessEntityId, principalTable: "AccessEntities",
                    principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });

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
                table.ForeignKey(name: "FK_AccessRolePermissions_AspNetRoles_RoleId",
                    column: x => x.RoleId, principalTable: "AspNetRoles",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_AccessEntities_Key", table: "AccessEntities", column: "Key", unique: true);
        migrationBuilder.CreateIndex(name: "IX_AccessEntities_DbContextName_ModelFullName", table: "AccessEntities",
            columns: new[] { "DbContextName", "ModelFullName" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_AccessPermissions_AccessEntityId_Key", table: "AccessPermissions",
            columns: new[] { "AccessEntityId", "Key" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_AccessRolePermissions_AccessPermissionId",
            table: "AccessRolePermissions", column: "AccessPermissionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AccessRolePermissions");
        migrationBuilder.DropTable(name: "AccessPermissions");
        migrationBuilder.DropTable(name: "AccessEntities");
    }
}
