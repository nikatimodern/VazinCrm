using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations;

[DbContext(typeof(UserContextDb))]
[Migration("20261123114500_AddAllExceptSelectedUsersScope")]
public partial class AddAllExceptSelectedUsersScope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"ALTER TABLE [AccessUserPermissions]
            DROP CONSTRAINT [CK_AccessUserPermissions_Scope]");
        migrationBuilder.Sql(@"ALTER TABLE [AccessUserPermissions]
            ADD CONSTRAINT [CK_AccessUserPermissions_Scope] CHECK ([Scope] IN (1, 2, 3, 4))");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Safe downgrade: exclude-list scopes revert to Own, never All.
        migrationBuilder.Sql("UPDATE [AccessUserPermissions] SET [Scope] = 1 WHERE [Scope] = 4");
        migrationBuilder.Sql(@"DELETE FROM [AccessUserScopeMembers]
            WHERE [AccessPermissionId] IN (SELECT [AccessPermissionId] FROM [AccessUserPermissions]
            WHERE [Scope] = 1)");
        migrationBuilder.Sql(@"ALTER TABLE [AccessUserPermissions]
            DROP CONSTRAINT [CK_AccessUserPermissions_Scope]");
        migrationBuilder.Sql(@"ALTER TABLE [AccessUserPermissions]
            ADD CONSTRAINT [CK_AccessUserPermissions_Scope] CHECK ([Scope] IN (1, 2, 3))");
    }
}
