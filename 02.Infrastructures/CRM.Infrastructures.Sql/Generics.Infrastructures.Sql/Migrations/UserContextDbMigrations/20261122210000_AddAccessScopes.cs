using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations;

[DbContext(typeof(UserContextDb))]
[Migration("20261122210000_AddAccessScopes")]
public partial class AddAccessScopes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "SupportsScope", table: "AccessPermissions",
            type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<byte>(name: "Scope", table: "AccessRolePermissions",
            type: "tinyint", nullable: false, defaultValue: (byte)2);
        migrationBuilder.AddColumn<byte>(name: "Scope", table: "AccessUserPermissions",
            type: "tinyint", nullable: false, defaultValue: (byte)2);
        // Existing permissions keep the exact previous effective grant (All).
        migrationBuilder.Sql(@"UPDATE [AccessPermissions] SET [SupportsScope] = 1
            WHERE [Key] IN (N'View', N'Edit', N'Delete')");
        migrationBuilder.Sql(@"ALTER TABLE [AccessRolePermissions]
            ADD CONSTRAINT [CK_AccessRolePermissions_Scope] CHECK ([Scope] IN (1, 2))");
        migrationBuilder.Sql(@"ALTER TABLE [AccessUserPermissions]
            ADD CONSTRAINT [CK_AccessUserPermissions_Scope] CHECK ([Scope] IN (1, 2))");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE [AccessUserPermissions] DROP CONSTRAINT [CK_AccessUserPermissions_Scope]");
        migrationBuilder.Sql("ALTER TABLE [AccessRolePermissions] DROP CONSTRAINT [CK_AccessRolePermissions_Scope]");
        migrationBuilder.DropColumn(name: "Scope", table: "AccessUserPermissions");
        migrationBuilder.DropColumn(name: "Scope", table: "AccessRolePermissions");
        migrationBuilder.DropColumn(name: "SupportsScope", table: "AccessPermissions");
    }
}
