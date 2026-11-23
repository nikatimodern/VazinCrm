/*
  PreFactor & Factor AccessCatalog setup (UserConnection / UserContextDb).
  Apply AFTER all AccessScopes migrations. Never run against CRMConnection.

  DEFAULT: @Apply = 0 -> READ-ONLY preview, no changes.
  Set @Apply = 1 to register two entities, 21 permissions and 81 initial
  role grants. Existing user overrides and existing grants are preserved.
  The three matching EF Core data migrations are idempotent; after this
  script they can be applied safely, without duplicating grants.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit = 0;

IF DB_ID() IS NULL OR OBJECT_ID(N'dbo.AccessUserPermissions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AccessUserScopeMembers', N'U') IS NULL
   OR COL_LENGTH(N'dbo.AccessRolePermissions', N'Scope') IS NULL
    THROW 51026, 'Apply the UserContextDb AccessCatalog/Scope migrations first.', 1;

IF @Apply = 0
BEGIN
    SELECT e.[Key] AS ExistingEntity, COUNT(p.Id) AS ExistingPermissions
    FROM dbo.AccessEntities e
    LEFT JOIN dbo.AccessPermissions p ON p.AccessEntityId=e.Id
    WHERE e.[Key] IN (N'PreFactor',N'Factor')
    GROUP BY e.[Key];

    SELECT r.RoleName, CASE WHEN ar.Id IS NULL THEN N'نقش یافت نشد' ELSE N'موجود' END AS RoleStatus,
           COUNT(ur.UserId) AS MemberCount
    FROM (VALUES (N'مدیر ارشد'),(N'مدیر عامل'),(N'مدیر اجرایی'),(N'مدیر فروش'),(N'کارشناس فروش'),(N'مدیر تامین'),(N'کارشناس تامین'),(N'مدیر مالی'),(N'کارشناس مالی')) r(RoleName)
    LEFT JOIN dbo.AspNetRoles ar ON ar.[Name]=r.RoleName
    LEFT JOIN dbo.AspNetUserRoles ur ON ur.RoleId=ar.Id
    GROUP BY r.RoleName,ar.Id
    ORDER BY r.RoleName;

    SELECT N'PreFactor' AS EntityKey, 13 AS ExpectedPermissions, 46 AS ExpectedRoleGrants
    UNION ALL SELECT N'Factor',8,35;
    PRINT N'PREVIEW ONLY: no data changed. Change @Apply to 1 to apply.';
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Phase 1: register AccessEntities
    IF EXISTS (SELECT 1 FROM dbo.AccessEntities WHERE [Key] = N'PreFactor'
               AND (DbContextName <> N'CRMContextDb' OR ModelFullName <> N'CRM.Core.Domain.PreFactors.Entities.PreFactor'))
        THROW 51021, 'AccessCatalog PreFactor key belongs to a different model.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AccessEntities WHERE [Key] = N'Factor'
               AND (DbContextName <> N'CRMContextDb' OR ModelFullName <> N'CRM.Core.Domain.Factors.Entities.Factor'))
        THROW 51022, 'AccessCatalog Factor key belongs to a different model.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AccessEntities WHERE DbContextName = N'CRMContextDb'
               AND ModelFullName = N'CRM.Core.Domain.PreFactors.Entities.PreFactor' AND [Key] <> N'PreFactor')
        THROW 51023, 'PreFactor model is already registered using another key.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AccessEntities WHERE DbContextName = N'CRMContextDb'
               AND ModelFullName = N'CRM.Core.Domain.Factors.Entities.Factor' AND [Key] <> N'Factor')
        THROW 51024, 'Factor model is already registered using another key.', 1;
    INSERT INTO dbo.AccessEntities (Id,[Key],Title,[Description],DbContextName,ModelFullName,IsActive,CreatedAtUtc)
    SELECT NEWID(),v.EntityKey,v.Title,v.[Description],N'CRMContextDb',v.ModelFullName,1,SYSUTCDATETIME()
    FROM (VALUES
      (N'PreFactor',N'پیش‌فاکتورها',N'مدیریت پیش‌فاکتور و گردش تأییدها',N'CRM.Core.Domain.PreFactors.Entities.PreFactor'),
      (N'Factor',N'فاکتورها',N'مدیریت فاکتور فروش و ارتباط مالی',N'CRM.Core.Domain.Factors.Entities.Factor')
    ) AS v(EntityKey,Title,[Description],ModelFullName)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.AccessEntities e WHERE e.[Key] = v.EntityKey);

    -- Phase 2: register AccessPermissions
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

    -- Phase 3: inherit permissions from existing Identity roles
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

    COMMIT TRANSACTION;
    PRINT N'AccessCatalog registration and role backfill applied successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- Review the effective grants using VerifyPreFactorFactorAccessCatalog.sql
