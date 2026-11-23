using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations;

[DbContext(typeof(UserContextDb))]
[Migration("20261123153000_RemoveCustomerNormalizePermission")]
public partial class RemoveCustomerNormalizePermission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // This permission was never meant to gate customer search maintenance.
        // Remove its grants first because SQL Server restricts deleting a
        // permission while role/user assignments still reference it.
        migrationBuilder.Sql(@"
DECLARE @ObsoletePermissions TABLE (Id uniqueidentifier NOT NULL PRIMARY KEY);

INSERT INTO @ObsoletePermissions (Id)
SELECT p.Id
FROM dbo.AccessPermissions AS p
INNER JOIN dbo.AccessEntities AS e ON e.Id = p.AccessEntityId
WHERE e.[Key] = N'Customer'
  AND p.[Key] IN (N'Normalize', N'Customer.Normalize');

DELETE scopeMember
FROM dbo.AccessUserScopeMembers AS scopeMember
INNER JOIN @ObsoletePermissions AS obsolete
    ON obsolete.Id = scopeMember.AccessPermissionId;

DELETE rolePermission
FROM dbo.AccessRolePermissions AS rolePermission
INNER JOIN @ObsoletePermissions AS obsolete
    ON obsolete.Id = rolePermission.AccessPermissionId;

DELETE userPermission
FROM dbo.AccessUserPermissions AS userPermission
INNER JOIN @ObsoletePermissions AS obsolete
    ON obsolete.Id = userPermission.AccessPermissionId;

DELETE permission
FROM dbo.AccessPermissions AS permission
INNER JOIN @ObsoletePermissions AS obsolete ON obsolete.Id = permission.Id;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Destructive catalog cleanup cannot safely reconstruct the previous
        // role grants or per-user overrides. Intentionally do not recreate it.
    }
}
