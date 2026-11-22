using System;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations;
[DbContext(typeof(UserContextDb))]
[Migration("20261122231500_AddSelectedUserAccessScope")]
public partial class AddSelectedUserAccessScope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE [AccessUserPermissions] DROP CONSTRAINT [CK_AccessUserPermissions_Scope]");
        migrationBuilder.Sql("ALTER TABLE [AccessUserPermissions] ADD CONSTRAINT [CK_AccessUserPermissions_Scope] CHECK ([Scope] IN (1, 2, 3))");
        migrationBuilder.CreateTable("AccessUserScopeMembers", table => new
        {
            UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
            AccessPermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            TargetUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
        }, table =>
        {
            table.PrimaryKey("PK_AccessUserScopeMembers", x => new { x.UserId, x.AccessPermissionId, x.TargetUserId });
            table.ForeignKey("FK_AccessUserScopeMembers_AccessUserPermissions_UserId_AccessPermissionId",
                x => new { x.UserId, x.AccessPermissionId }, "AccessUserPermissions",
                new[] { "UserId", "AccessPermissionId" }, onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_AccessUserScopeMembers_TargetUserId", "AccessUserScopeMembers", "TargetUserId");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AccessUserScopeMembers");
        migrationBuilder.Sql("UPDATE [AccessUserPermissions] SET [Scope] = 1 WHERE [Scope] = 3");
        migrationBuilder.Sql("ALTER TABLE [AccessUserPermissions] DROP CONSTRAINT [CK_AccessUserPermissions_Scope]");
        migrationBuilder.Sql("ALTER TABLE [AccessUserPermissions] ADD CONSTRAINT [CK_AccessUserPermissions_Scope] CHECK ([Scope] IN (1, 2))");
    }
}
