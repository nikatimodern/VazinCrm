using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations;

[DbContext(typeof(UserContextDb))]
[Migration("20261123234100_RegisterPreFactorFactorPermissions")]
public partial class RegisterPreFactorFactorPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Idempotent data migration: do not overwrite customized catalog entries.
        migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM dbo.AccessEntities WHERE [Key] = N'PreFactor')
   OR NOT EXISTS (SELECT 1 FROM dbo.AccessEntities WHERE [Key] = N'Factor')
    THROW 51025, 'Register AccessCatalog entities before their permissions.', 1;
INSERT INTO dbo.AccessPermissions
    (Id, AccessEntityId, [Key], Title, [Description], IsActive, SupportsScope, CreatedAtUtc)
SELECT NEWID(),e.Id,v.PermissionKey,v.Title,v.[Description],1,CONVERT(bit,v.SupportsScope),SYSUTCDATETIME()
FROM (VALUES
  (N'PreFactor',N'View',N'مشاهده',N'مشاهده پیش‌فاکتور و اقلام آن',1),
  (N'PreFactor',N'Create',N'ایجاد',N'ثبت پیش‌فاکتور جدید',0),
  (N'PreFactor',N'Edit',N'ویرایش',N'ویرایش اطلاعات عمومی پیش‌فاکتور',1),
  (N'PreFactor',N'Delete',N'حذف',N'حذف پیش‌فاکتور',1),
  (N'PreFactor',N'AssignTamin',N'واگذاری تأمین',N'ارجاع و تعیین کارشناس تأمین',1),
  (N'PreFactor',N'ApproveTamin',N'تأیید تأمین',N'تأیید یا رد مرحله تأمین',1),
  (N'PreFactor',N'ApproveSales',N'تأیید مدیر فروش',N'تأیید یا رد توسط مدیر فروش',0),
  (N'PreFactor',N'ApproveFinancial',N'تأیید مالی',N'تأیید یا رد مالی پیش‌فاکتور',0),
  (N'PreFactor',N'SendToCustomer',N'ارسال به مشتری',N'ارسال یا ارسال مجدد پیش‌فاکتور برای مشتری',1),
  (N'PreFactor',N'ApproveCustomerManual',N'تأیید دستی مشتری',N'تأیید دستی پیش‌فاکتور به جای مشتری',0),
  (N'PreFactor',N'ChangeStatus',N'تغییر وضعیت',N'برگرداندن یا تغییر مرحله گردش کار پیش‌فاکتور',1),
  (N'PreFactor',N'EditSupply',N'ویرایش اقلام تأمین',N'ویرایش اقلام و اطلاعات مربوط به تأمین',1),
  (N'PreFactor',N'ExportExternal',N'ارسال به حسابداری',N'ارسال پیش‌فاکتور به نرم‌افزار حسابداری',0),
  (N'Factor',N'View',N'مشاهده',N'مشاهده فاکتورها و جزئیات آن‌ها',1),
  (N'Factor',N'Create',N'ایجاد',N'ثبت فاکتور فروش جدید',0),
  (N'Factor',N'Edit',N'ویرایش',N'ویرایش فاکتور فروش',1),
  (N'Factor',N'Delete',N'حذف',N'حذف یا ابطال فیزیکی فاکتور با کنترل مالی',1),
  (N'Factor',N'Approve',N'تأیید',N'تأیید نهایی فاکتور',0),
  (N'Factor',N'Cancel',N'ابطال',N'ابطال یا بازگشت فاکتور از تأیید',0),
  (N'Factor',N'EditFinancial',N'اصلاح اطلاعات مالی',N'اصلاح شماره و ارتباط مالی فاکتور',0),
  (N'Factor',N'ExportExternal',N'ارسال به حسابداری',N'ارسال فاکتور به نرم‌افزار حسابداری',0)
) AS v(EntityKey, PermissionKey, Title, [Description], SupportsScope)
JOIN dbo.AccessEntities e ON e.[Key] = v.EntityKey
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.AccessPermissions p
    WHERE p.AccessEntityId = e.Id AND p.[Key] = v.PermissionKey
);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally keep permission data and administrator changes on rollback.
        // Remove catalog entries only through the audited AccessCatalog UI.
    }
}
