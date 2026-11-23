using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations;

[DbContext(typeof(UserContextDb))]
[Migration("20261123234200_SeedPreFactorFactorRoleGrants")]
public partial class SeedPreFactorFactorRoleGrants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Idempotent data migration: do not overwrite customized catalog entries.
        migrationBuilder.Sql("""
-- Users inherit these grants through dbo.AspNetUserRoles. Never copy role grants
-- into dbo.AccessUserPermissions: that table stores *only* explicit overrides.
-- Existing personalized allow/deny overrides are left intact.
INSERT INTO dbo.AccessRolePermissions (RoleId, AccessPermissionId, Scope)
SELECT r.Id,p.Id,CONVERT(tinyint,v.Scope)
FROM (VALUES
  (N'مدیر ارشد',N'PreFactor',N'View',2),
  (N'مدیر ارشد',N'PreFactor',N'Create',2),
  (N'مدیر ارشد',N'PreFactor',N'Edit',2),
  (N'مدیر ارشد',N'PreFactor',N'Delete',2),
  (N'مدیر ارشد',N'PreFactor',N'AssignTamin',2),
  (N'مدیر ارشد',N'PreFactor',N'ApproveTamin',2),
  (N'مدیر ارشد',N'PreFactor',N'ApproveSales',2),
  (N'مدیر ارشد',N'PreFactor',N'ApproveFinancial',2),
  (N'مدیر ارشد',N'PreFactor',N'SendToCustomer',2),
  (N'مدیر ارشد',N'PreFactor',N'ApproveCustomerManual',2),
  (N'مدیر ارشد',N'PreFactor',N'ChangeStatus',2),
  (N'مدیر ارشد',N'PreFactor',N'EditSupply',2),
  (N'مدیر ارشد',N'PreFactor',N'ExportExternal',2),
  (N'مدیر ارشد',N'Factor',N'View',2),
  (N'مدیر ارشد',N'Factor',N'Create',2),
  (N'مدیر ارشد',N'Factor',N'Edit',2),
  (N'مدیر ارشد',N'Factor',N'Delete',2),
  (N'مدیر ارشد',N'Factor',N'Approve',2),
  (N'مدیر ارشد',N'Factor',N'Cancel',2),
  (N'مدیر ارشد',N'Factor',N'EditFinancial',2),
  (N'مدیر ارشد',N'Factor',N'ExportExternal',2),
  (N'مدیر عامل',N'PreFactor',N'View',2),
  (N'مدیر عامل',N'PreFactor',N'ApproveSales',2),
  (N'مدیر عامل',N'PreFactor',N'ExportExternal',2),
  (N'مدیر عامل',N'Factor',N'View',2),
  (N'مدیر عامل',N'Factor',N'ExportExternal',2),
  (N'مدیر اجرایی',N'PreFactor',N'View',2),
  (N'مدیر اجرایی',N'PreFactor',N'AssignTamin',2),
  (N'مدیر اجرایی',N'PreFactor',N'ChangeStatus',2),
  (N'مدیر اجرایی',N'Factor',N'View',2),
  (N'مدیر فروش',N'PreFactor',N'View',2),
  (N'مدیر فروش',N'PreFactor',N'Create',2),
  (N'مدیر فروش',N'PreFactor',N'Edit',2),
  (N'مدیر فروش',N'PreFactor',N'Delete',2),
  (N'مدیر فروش',N'PreFactor',N'AssignTamin',2),
  (N'مدیر فروش',N'PreFactor',N'ApproveSales',2),
  (N'مدیر فروش',N'PreFactor',N'SendToCustomer',2),
  (N'مدیر فروش',N'PreFactor',N'ApproveCustomerManual',2),
  (N'مدیر فروش',N'PreFactor',N'ChangeStatus',2),
  (N'مدیر فروش',N'Factor',N'View',2),
  (N'مدیر فروش',N'Factor',N'Create',2),
  (N'مدیر فروش',N'Factor',N'Edit',2),
  (N'کارشناس فروش',N'PreFactor',N'View',1),
  (N'کارشناس فروش',N'PreFactor',N'Create',2),
  (N'کارشناس فروش',N'PreFactor',N'Edit',1),
  (N'کارشناس فروش',N'PreFactor',N'SendToCustomer',1),
  (N'کارشناس فروش',N'Factor',N'View',1),
  (N'کارشناس فروش',N'Factor',N'Create',2),
  (N'کارشناس فروش',N'Factor',N'Edit',1),
  (N'مدیر تامین',N'PreFactor',N'View',2),
  (N'مدیر تامین',N'PreFactor',N'AssignTamin',2),
  (N'مدیر تامین',N'PreFactor',N'ApproveTamin',2),
  (N'مدیر تامین',N'PreFactor',N'EditSupply',2),
  (N'مدیر تامین',N'PreFactor',N'ChangeStatus',2),
  (N'مدیر تامین',N'Factor',N'View',2),
  (N'کارشناس تامین',N'PreFactor',N'View',1),
  (N'کارشناس تامین',N'PreFactor',N'ApproveTamin',1),
  (N'کارشناس تامین',N'PreFactor',N'EditSupply',1),
  (N'کارشناس تامین',N'Factor',N'View',1),
  (N'مدیر مالی',N'PreFactor',N'View',2),
  (N'مدیر مالی',N'PreFactor',N'ApproveFinancial',2),
  (N'مدیر مالی',N'PreFactor',N'ExportExternal',2),
  (N'مدیر مالی',N'Factor',N'View',2),
  (N'مدیر مالی',N'Factor',N'Create',2),
  (N'مدیر مالی',N'Factor',N'Edit',2),
  (N'مدیر مالی',N'Factor',N'Delete',2),
  (N'مدیر مالی',N'Factor',N'Approve',2),
  (N'مدیر مالی',N'Factor',N'Cancel',2),
  (N'مدیر مالی',N'Factor',N'EditFinancial',2),
  (N'مدیر مالی',N'Factor',N'ExportExternal',2),
  (N'کارشناس مالی',N'PreFactor',N'View',2),
  (N'کارشناس مالی',N'PreFactor',N'ApproveFinancial',2),
  (N'کارشناس مالی',N'PreFactor',N'ExportExternal',2),
  (N'کارشناس مالی',N'Factor',N'View',2),
  (N'کارشناس مالی',N'Factor',N'Create',2),
  (N'کارشناس مالی',N'Factor',N'Edit',2),
  (N'کارشناس مالی',N'Factor',N'Delete',2),
  (N'کارشناس مالی',N'Factor',N'Approve',2),
  (N'کارشناس مالی',N'Factor',N'Cancel',2),
  (N'کارشناس مالی',N'Factor',N'EditFinancial',2),
  (N'کارشناس مالی',N'Factor',N'ExportExternal',2)
) AS v(RoleName,EntityKey,PermissionKey,Scope)
JOIN dbo.AspNetRoles r ON r.[Name] = v.RoleName
JOIN dbo.AccessEntities e ON e.[Key] = v.EntityKey AND e.IsActive = 1
JOIN dbo.AccessPermissions p ON p.AccessEntityId = e.Id
                             AND p.[Key] = v.PermissionKey AND p.IsActive = 1
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.AccessRolePermissions g
    WHERE g.RoleId = r.Id AND g.AccessPermissionId = p.Id
);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally keep permission data and administrator changes on rollback.
        // Remove catalog entries only through the audited AccessCatalog UI.
    }
}
