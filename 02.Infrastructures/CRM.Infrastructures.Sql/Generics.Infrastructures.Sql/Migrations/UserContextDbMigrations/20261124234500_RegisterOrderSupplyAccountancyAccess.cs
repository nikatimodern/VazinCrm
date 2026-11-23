using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations;

[DbContext(typeof(UserContextDb))]
[Migration("20261124234500_RegisterOrderSupplyAccountancyAccess")]
public partial class RegisterOrderSupplyAccountancyAccess : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM dbo.AccessEntities e JOIN (VALUES (N'OrderHead',N'حواله‌ها',N'CRM.Core.Domain.Orders.Entities.OrderHead'),
(N'Supply',N'بهای تأمین',N'CRM.Core.Domain.Supplies.Entities.Supply'),
(N'SupplyFactor',N'فاکتورهای تأمین',N'CRM.Core.Domain.Supplies.Entities.SupplyFactor'),
(N'Accountancy',N'واریزی‌ها',N'CRM.Core.Domain.Accountancies.Entities.Accountancy')) v(K,T,M) ON e.[Key]=v.K OR (e.DbContextName=N'CRMContextDb' AND e.ModelFullName=v.M) WHERE e.[Key]<>v.K OR e.DbContextName<>N'CRMContextDb' OR e.ModelFullName<>v.M)
    THROW 51041, 'Entity key/model collision.', 1;
INSERT INTO dbo.AccessEntities(Id,[Key],Title,[Description],DbContextName,ModelFullName,IsActive,CreatedAtUtc)
SELECT NEWID(),v.K,v.T,v.T,N'CRMContextDb',v.M,1,SYSUTCDATETIME()
FROM (VALUES (N'OrderHead',N'حواله‌ها',N'CRM.Core.Domain.Orders.Entities.OrderHead'),
(N'Supply',N'بهای تأمین',N'CRM.Core.Domain.Supplies.Entities.Supply'),
(N'SupplyFactor',N'فاکتورهای تأمین',N'CRM.Core.Domain.Supplies.Entities.SupplyFactor'),
(N'Accountancy',N'واریزی‌ها',N'CRM.Core.Domain.Accountancies.Entities.Accountancy')) v(K,T,M)
WHERE NOT EXISTS(SELECT 1 FROM dbo.AccessEntities e WHERE e.[Key]=v.K);

INSERT INTO dbo.AccessPermissions(Id,AccessEntityId,[Key],Title,[Description],IsActive,SupportsScope,CreatedAtUtc)
SELECT NEWID(),e.Id,v.K,v.T,v.T,1,CONVERT(bit,v.S),SYSUTCDATETIME()
FROM (VALUES (N'OrderHead',N'View',N'مشاهده',1),
(N'OrderHead',N'Create',N'ایجاد',0),
(N'OrderHead',N'Edit',N'ویرایش',1),
(N'OrderHead',N'Delete',N'حذف',1),
(N'OrderHead',N'ChangeStatus',N'تغییر وضعیت',1),
(N'Supply',N'View',N'مشاهده',1),
(N'Supply',N'Create',N'ایجاد',0),
(N'Supply',N'Edit',N'ویرایش',1),
(N'Supply',N'Delete',N'حذف',1),
(N'SupplyFactor',N'View',N'مشاهده',1),
(N'SupplyFactor',N'Create',N'ایجاد',0),
(N'SupplyFactor',N'Edit',N'ویرایش',1),
(N'SupplyFactor',N'Delete',N'حذف',1),
(N'SupplyFactor',N'ManageAllocations',N'مدیریت تخصیص‌ها',1),
(N'SupplyFactor',N'ExportExternal',N'ارسال به حسابداری',0),
(N'Accountancy',N'View',N'مشاهده',1),
(N'Accountancy',N'Create',N'ایجاد',0),
(N'Accountancy',N'Edit',N'ویرایش',1),
(N'Accountancy',N'Delete',N'حذف',1),
(N'Accountancy',N'Approve',N'تأیید و رد',1),
(N'Accountancy',N'ChangeStatus',N'ارسال برای تأیید',1),
(N'Accountancy',N'Settle',N'وصول و لغو وصول',1),
(N'Accountancy',N'Transfer',N'انتقال واریزی',1),
(N'Accountancy',N'UseWallet',N'استفاده از کیف پول',1)) v(E,K,T,S)
JOIN dbo.AccessEntities e ON e.[Key]=v.E
WHERE NOT EXISTS(SELECT 1 FROM dbo.AccessPermissions p WHERE p.AccessEntityId=e.Id AND p.[Key]=v.K);

INSERT INTO dbo.AccessRolePermissions(RoleId,AccessPermissionId,Scope)
SELECT r.Id,p.Id,CONVERT(tinyint,v.S)
FROM (VALUES (N'مدیر ارشد',N'OrderHead',N'View',2),
(N'مدیر ارشد',N'OrderHead',N'Create',2),
(N'مدیر ارشد',N'OrderHead',N'Edit',2),
(N'مدیر ارشد',N'OrderHead',N'Delete',2),
(N'مدیر ارشد',N'OrderHead',N'ChangeStatus',2),
(N'مدیر ارشد',N'Supply',N'View',2),
(N'مدیر ارشد',N'Supply',N'Create',2),
(N'مدیر ارشد',N'Supply',N'Edit',2),
(N'مدیر ارشد',N'Supply',N'Delete',2),
(N'مدیر ارشد',N'SupplyFactor',N'View',2),
(N'مدیر ارشد',N'SupplyFactor',N'Create',2),
(N'مدیر ارشد',N'SupplyFactor',N'Edit',2),
(N'مدیر ارشد',N'SupplyFactor',N'Delete',2),
(N'مدیر ارشد',N'SupplyFactor',N'ManageAllocations',2),
(N'مدیر ارشد',N'SupplyFactor',N'ExportExternal',2),
(N'مدیر ارشد',N'Accountancy',N'View',2),
(N'مدیر ارشد',N'Accountancy',N'Create',2),
(N'مدیر ارشد',N'Accountancy',N'Edit',2),
(N'مدیر ارشد',N'Accountancy',N'Delete',2),
(N'مدیر ارشد',N'Accountancy',N'Approve',2),
(N'مدیر ارشد',N'Accountancy',N'ChangeStatus',2),
(N'مدیر ارشد',N'Accountancy',N'Settle',2),
(N'مدیر ارشد',N'Accountancy',N'Transfer',2),
(N'مدیر ارشد',N'Accountancy',N'UseWallet',2),
(N'مدیر عامل',N'OrderHead',N'View',2),
(N'مدیر عامل',N'Supply',N'View',2),
(N'مدیر عامل',N'SupplyFactor',N'View',2),
(N'مدیر عامل',N'SupplyFactor',N'ExportExternal',2),
(N'مدیر عامل',N'Accountancy',N'View',2),
(N'مدیر اجرایی',N'OrderHead',N'View',2),
(N'مدیر اجرایی',N'Supply',N'View',2),
(N'مدیر اجرایی',N'SupplyFactor',N'View',2),
(N'مدیر اجرایی',N'Accountancy',N'View',2),
(N'مدیر فروش',N'OrderHead',N'View',2),
(N'مدیر فروش',N'OrderHead',N'Create',2),
(N'مدیر فروش',N'OrderHead',N'Edit',2),
(N'مدیر فروش',N'OrderHead',N'Delete',2),
(N'مدیر فروش',N'OrderHead',N'ChangeStatus',2),
(N'مدیر فروش',N'Supply',N'View',2),
(N'مدیر فروش',N'Accountancy',N'View',2),
(N'مدیر فروش',N'Accountancy',N'Create',2),
(N'مدیر فروش',N'Accountancy',N'Edit',1),
(N'مدیر فروش',N'Accountancy',N'Delete',1),
(N'مدیر فروش',N'Accountancy',N'ChangeStatus',1),
(N'مدیر فروش',N'Accountancy',N'UseWallet',1),
(N'کارشناس فروش',N'OrderHead',N'View',1),
(N'کارشناس فروش',N'OrderHead',N'Create',2),
(N'کارشناس فروش',N'OrderHead',N'Edit',1),
(N'کارشناس فروش',N'OrderHead',N'Delete',1),
(N'کارشناس فروش',N'OrderHead',N'ChangeStatus',1),
(N'کارشناس فروش',N'Supply',N'View',1),
(N'کارشناس فروش',N'Accountancy',N'View',1),
(N'کارشناس فروش',N'Accountancy',N'Create',2),
(N'کارشناس فروش',N'Accountancy',N'Edit',1),
(N'کارشناس فروش',N'Accountancy',N'Delete',1),
(N'کارشناس فروش',N'Accountancy',N'ChangeStatus',1),
(N'کارشناس فروش',N'Accountancy',N'UseWallet',1),
(N'مدیر تامین',N'OrderHead',N'View',2),
(N'مدیر تامین',N'OrderHead',N'Create',2),
(N'مدیر تامین',N'OrderHead',N'Edit',2),
(N'مدیر تامین',N'OrderHead',N'Delete',2),
(N'مدیر تامین',N'OrderHead',N'ChangeStatus',2),
(N'مدیر تامین',N'Supply',N'View',2),
(N'مدیر تامین',N'Supply',N'Create',2),
(N'مدیر تامین',N'Supply',N'Edit',2),
(N'مدیر تامین',N'Supply',N'Delete',2),
(N'مدیر تامین',N'SupplyFactor',N'View',2),
(N'مدیر تامین',N'SupplyFactor',N'Create',2),
(N'مدیر تامین',N'SupplyFactor',N'Edit',2),
(N'مدیر تامین',N'SupplyFactor',N'Delete',2),
(N'مدیر تامین',N'SupplyFactor',N'ManageAllocations',2),
(N'مدیر تامین',N'Accountancy',N'View',2),
(N'کارشناس تامین',N'OrderHead',N'View',1),
(N'کارشناس تامین',N'OrderHead',N'Create',2),
(N'کارشناس تامین',N'OrderHead',N'Edit',1),
(N'کارشناس تامین',N'OrderHead',N'Delete',1),
(N'کارشناس تامین',N'OrderHead',N'ChangeStatus',1),
(N'کارشناس تامین',N'Supply',N'View',1),
(N'کارشناس تامین',N'Supply',N'Create',2),
(N'کارشناس تامین',N'Supply',N'Edit',1),
(N'کارشناس تامین',N'Supply',N'Delete',1),
(N'کارشناس تامین',N'SupplyFactor',N'View',2),
(N'کارشناس تامین',N'SupplyFactor',N'Create',2),
(N'کارشناس تامین',N'SupplyFactor',N'Edit',1),
(N'کارشناس تامین',N'SupplyFactor',N'Delete',1),
(N'کارشناس تامین',N'SupplyFactor',N'ManageAllocations',1),
(N'کارشناس تامین',N'Accountancy',N'View',1),
(N'کارشناس تامین',N'Accountancy',N'Create',2),
(N'کارشناس تامین',N'Accountancy',N'ChangeStatus',1),
(N'مدیر مالی',N'OrderHead',N'View',2),
(N'مدیر مالی',N'Supply',N'View',2),
(N'مدیر مالی',N'SupplyFactor',N'View',2),
(N'مدیر مالی',N'SupplyFactor',N'Create',2),
(N'مدیر مالی',N'SupplyFactor',N'Edit',2),
(N'مدیر مالی',N'SupplyFactor',N'Delete',2),
(N'مدیر مالی',N'SupplyFactor',N'ManageAllocations',2),
(N'مدیر مالی',N'SupplyFactor',N'ExportExternal',2),
(N'مدیر مالی',N'Accountancy',N'View',2),
(N'مدیر مالی',N'Accountancy',N'Create',2),
(N'مدیر مالی',N'Accountancy',N'Edit',2),
(N'مدیر مالی',N'Accountancy',N'Delete',2),
(N'مدیر مالی',N'Accountancy',N'Approve',2),
(N'مدیر مالی',N'Accountancy',N'ChangeStatus',2),
(N'مدیر مالی',N'Accountancy',N'Settle',2),
(N'مدیر مالی',N'Accountancy',N'Transfer',2),
(N'مدیر مالی',N'Accountancy',N'UseWallet',2),
(N'کارشناس مالی',N'OrderHead',N'View',2),
(N'کارشناس مالی',N'Supply',N'View',2),
(N'کارشناس مالی',N'SupplyFactor',N'View',2),
(N'کارشناس مالی',N'SupplyFactor',N'Create',2),
(N'کارشناس مالی',N'SupplyFactor',N'Edit',2),
(N'کارشناس مالی',N'SupplyFactor',N'Delete',2),
(N'کارشناس مالی',N'SupplyFactor',N'ManageAllocations',2),
(N'کارشناس مالی',N'SupplyFactor',N'ExportExternal',2),
(N'کارشناس مالی',N'Accountancy',N'View',2),
(N'کارشناس مالی',N'Accountancy',N'Create',2),
(N'کارشناس مالی',N'Accountancy',N'Edit',2),
(N'کارشناس مالی',N'Accountancy',N'Delete',2),
(N'کارشناس مالی',N'Accountancy',N'Approve',2),
(N'کارشناس مالی',N'Accountancy',N'ChangeStatus',2),
(N'کارشناس مالی',N'Accountancy',N'Settle',2),
(N'کارشناس مالی',N'Accountancy',N'Transfer',2),
(N'کارشناس مالی',N'Accountancy',N'UseWallet',2)) v(R,E,K,S)
JOIN dbo.AspNetRoles r ON r.Name=v.R
JOIN dbo.AccessEntities e ON e.[Key]=v.E AND e.IsActive=1
JOIN dbo.AccessPermissions p ON p.AccessEntityId=e.Id AND p.[Key]=v.K AND p.IsActive=1
WHERE NOT EXISTS(SELECT 1 FROM dbo.AccessRolePermissions rp WHERE rp.RoleId=r.Id AND rp.AccessPermissionId=p.Id);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Preserve explicit administrator settings and access history.
    }
}
