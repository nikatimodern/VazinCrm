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
        // Safe downgrade: remove exclusion links only for scope 4, then revert
        // those permissions to Own. Never silently widen them to All.
        migrationBuilder.Sql(@"DELETE FROM [AccessUserScopeMembers] WHERE EXISTS (
            SELECT 1 FROM [AccessUserPermissions] AS [p]
            WHERE [p].[UserId] = [AccessUserScopeMembers].[UserId]
            AND [p].[AccessPermissionId] = [AccessUserScopeMembers].[AccessPermissionId]
            AND [p].[Scope] = 4)");
        migrationBuilder.Sql("UPDATE [AccessUserPermissions] SET [Scope] = 1 WHERE [Scope] = 4");
        migrationBuilder.Sql(@"ALTER TABLE [AccessUserPermissions]
            DROP CONSTRAINT [CK_AccessUserPermissions_Scope]");
        migrationBuilder.Sql(@"ALTER TABLE [AccessUserPermissions]
            ADD CONSTRAINT [CK_AccessUserPermissions_Scope] CHECK ([Scope] IN (1, 2, 3))");
    }
}
