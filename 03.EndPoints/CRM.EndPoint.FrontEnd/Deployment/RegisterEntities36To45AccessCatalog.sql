-- UserConnection only. Entities 36–45; staged registration and grant preview.
-- Insert missing registrations only; existing permissions, role grants and explicit overrides are retained.
-- ViewUserEvents authorizes registration/login/logout audit metadata only; raw user changes are never returned.
-- ExternalMapping requires unrestricted source View in addition to its existing global grants.
-- Review outstanding runtime and indirect-path checks before applying.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit=0;
IF OBJECT_ID(N'dbo.AccessRolePermissions',N'U') IS NULL OR COL_LENGTH(N'dbo.AccessRolePermissions',N'Scope') IS NULL THROW 51080,'Apply access catalog migrations first.',1;
DECLARE @Entities TABLE(EntityKey nvarchar(128) PRIMARY KEY, Title nvarchar(256), ModelFullName nvarchar(512));
INSERT INTO @Entities VALUES
(N'Agent',N'نمایندگان',N'CRM.Core.Domain.Customers.Entities.Agent'),
(N'ReleaseNote',N'گزارش نسخه‌ها',N'CRM.Core.Domain.ReleaseNotes.Entities.ReleaseNote'),
(N'LogSystem',N'گزارش تغییرات',N'CRM.Core.Domain.LogSystems.Entities.LogSystem'),
(N'SystemWarning',N'هشدارهای سیستم',N'CRM.Core.Domain.SystemWarnings.Entities.SystemWarning'),
(N'Company',N'اطلاعات شرکت',N'CRM.Core.Domain.Company.Entities.Company'),
(N'ASetting',N'تنظیمات سیستم',N'CRM.Core.Domain.ASettings.Entities.ASetting'),
(N'BaseItem',N'اطلاعات پایه',N'CRM.Core.Domain.BaseItems.Entities.BaseItem'),
(N'BaseDescription',N'توضیحات پایه',N'CRM.Core.Domain.PreFactors.Entities.BaseDescription'),
(N'ExternalSoftware',N'نرم‌افزارهای خارجی',N'CRM.Core.Domain.ExternalSoftwares.Entites.ExternalSoftware'),
(N'ExternalMapping',N'مدیریت تطبیق اطلاعات',N'CRM.Core.Domain.ExternalSoftwares.Entites.ExternalMapping');
DECLARE @Permissions TABLE(EntityKey nvarchar(128), PermissionKey nvarchar(128), Title nvarchar(256), SupportsScope bit, PRIMARY KEY(EntityKey,PermissionKey));
INSERT INTO @Permissions VALUES
(N'Agent',N'View',N'مشاهده',1),
(N'Agent',N'Create',N'ایجاد',0),
(N'Agent',N'Edit',N'ویرایش',1),
(N'Agent',N'Delete',N'حذف',1),
(N'ReleaseNote',N'View',N'مشاهده',0),
(N'ReleaseNote',N'Create',N'ایجاد',0),
(N'ReleaseNote',N'Edit',N'ویرایش',0),
(N'ReleaseNote',N'Delete',N'حذف',0),
(N'ReleaseNote',N'Publish',N'انتشار',0),
(N'ReleaseNote',N'Archive',N'بایگانی',0),
(N'LogSystem',N'View',N'مشاهده',0),
(N'LogSystem',N'ViewDetails',N'مشاهده جزئیات',0),
(N'LogSystem',N'ViewUserEvents',N'مشاهده رویدادهای ورود و خروج کاربران',0),
(N'SystemWarning',N'View',N'مشاهده',0),
(N'SystemWarning',N'Delete',N'حذف',0),
(N'SystemWarning',N'Refresh',N'به‌روزرسانی هشدارها',0),
(N'SystemWarning',N'ViewQualityControl',N'مشاهده هشدارهای کنترل کیفیت',0),
(N'Company',N'View',N'مشاهده',0),
(N'Company',N'Create',N'ایجاد',0),
(N'Company',N'Edit',N'ویرایش',0),
(N'Company',N'Delete',N'حذف',0),
(N'ASetting',N'View',N'مشاهده',0),
(N'ASetting',N'Edit',N'ویرایش',0),
(N'BaseItem',N'View',N'مشاهده',0),
(N'BaseItem',N'Create',N'ایجاد',0),
(N'BaseItem',N'Edit',N'ویرایش',0),
(N'BaseItem',N'Delete',N'حذف',0),
(N'BaseDescription',N'View',N'مشاهده',0),
(N'BaseDescription',N'Create',N'ایجاد',0),
(N'BaseDescription',N'Edit',N'ویرایش',0),
(N'BaseDescription',N'Delete',N'حذف',0),
(N'ExternalSoftware',N'View',N'مشاهده',0),
(N'ExternalSoftware',N'Create',N'ایجاد',0),
(N'ExternalSoftware',N'Edit',N'ویرایش',0),
(N'ExternalSoftware',N'Delete',N'حذف',0),
(N'ExternalSoftware',N'ManageSettings',N'مدیریت تنظیمات نرم‌افزار',0),
(N'ExternalMapping',N'View',N'مشاهده',0),
(N'ExternalMapping',N'Create',N'ایجاد',0),
(N'ExternalMapping',N'Edit',N'ویرایش',0),
(N'ExternalMapping',N'Delete',N'حذف',0);
DECLARE @Grants TABLE(RoleName nvarchar(256), EntityKey nvarchar(128), PermissionKey nvarchar(128), Scope tinyint, PRIMARY KEY(RoleName,EntityKey,PermissionKey));
INSERT INTO @Grants VALUES
(N'مدیر ارشد',N'Agent',N'View',2),
(N'مدیر ارشد',N'Agent',N'Create',2),
(N'مدیر ارشد',N'Agent',N'Edit',2),
(N'مدیر ارشد',N'Agent',N'Delete',2),
(N'مدیر ارشد',N'ReleaseNote',N'View',2),
(N'مدیر ارشد',N'ReleaseNote',N'Create',2),
(N'مدیر ارشد',N'ReleaseNote',N'Edit',2),
(N'مدیر ارشد',N'ReleaseNote',N'Delete',2),
(N'مدیر ارشد',N'ReleaseNote',N'Publish',2),
(N'مدیر ارشد',N'ReleaseNote',N'Archive',2),
(N'مدیر ارشد',N'LogSystem',N'View',2),
(N'مدیر ارشد',N'LogSystem',N'ViewDetails',2),
(N'مدیر ارشد',N'LogSystem',N'ViewUserEvents',2),
(N'مدیر ارشد',N'SystemWarning',N'View',2),
(N'مدیر ارشد',N'SystemWarning',N'Delete',2),
(N'مدیر ارشد',N'SystemWarning',N'Refresh',2),
(N'مدیر ارشد',N'SystemWarning',N'ViewQualityControl',2),
(N'مدیر مالی',N'SystemWarning',N'View',2),
(N'مدیر مالی',N'SystemWarning',N'Delete',2),
(N'مدیر مالی',N'SystemWarning',N'Refresh',2),
(N'کارشناس مالی',N'SystemWarning',N'View',2),
(N'کارشناس مالی',N'SystemWarning',N'Delete',2),
(N'کارشناس مالی',N'SystemWarning',N'Refresh',2),
(N'مدیر عامل',N'LogSystem',N'View',2),
(N'مدیر اجرایی',N'LogSystem',N'View',2),
(N'کارشناس فروش',N'LogSystem',N'View',2),
(N'مدیر فروش',N'LogSystem',N'View',2),
(N'کارشناس تامین',N'LogSystem',N'View',2),
(N'مدیر تامین',N'LogSystem',N'View',2),
(N'کارشناس مالی',N'LogSystem',N'View',2),
(N'مدیر مالی',N'LogSystem',N'View',2),
(N'مدیر ارشد',N'Company',N'View',2),
(N'مدیر ارشد',N'Company',N'Create',2),
(N'مدیر ارشد',N'Company',N'Edit',2),
(N'مدیر ارشد',N'Company',N'Delete',2),
(N'مدیر ارشد',N'ASetting',N'View',2),
(N'مدیر ارشد',N'ASetting',N'Edit',2),
(N'مدیر ارشد',N'BaseItem',N'View',2),
(N'مدیر ارشد',N'BaseItem',N'Create',2),
(N'مدیر ارشد',N'BaseItem',N'Edit',2),
(N'مدیر ارشد',N'BaseItem',N'Delete',2),
(N'مدیر ارشد',N'BaseDescription',N'View',2),
(N'مدیر ارشد',N'BaseDescription',N'Create',2),
(N'مدیر ارشد',N'BaseDescription',N'Edit',2),
(N'مدیر ارشد',N'BaseDescription',N'Delete',2),
(N'مدیر ارشد',N'ExternalSoftware',N'View',2),
(N'مدیر ارشد',N'ExternalSoftware',N'Create',2),
(N'مدیر ارشد',N'ExternalSoftware',N'Edit',2),
(N'مدیر ارشد',N'ExternalSoftware',N'Delete',2),
(N'مدیر ارشد',N'ExternalSoftware',N'ManageSettings',2),
(N'مدیر ارشد',N'ExternalMapping',N'View',2),
(N'مدیر ارشد',N'ExternalMapping',N'Create',2),
(N'مدیر ارشد',N'ExternalMapping',N'Edit',2),
(N'مدیر ارشد',N'ExternalMapping',N'Delete',2),
(N'مدیر فروش',N'Company',N'View',2),
(N'مدیر فروش',N'BaseItem',N'View',2),
(N'مدیر فروش',N'BaseDescription',N'View',2),
(N'کارشناس فروش',N'Company',N'View',2),
(N'کارشناس فروش',N'BaseItem',N'View',2),
(N'کارشناس فروش',N'BaseDescription',N'View',2),
(N'مدیر تامین',N'Company',N'View',2),
(N'مدیر تامین',N'BaseItem',N'View',2),
(N'مدیر تامین',N'BaseDescription',N'View',2),
(N'کارشناس تامین',N'Company',N'View',2),
(N'کارشناس تامین',N'BaseItem',N'View',2),
(N'کارشناس تامین',N'BaseDescription',N'View',2),
(N'مدیر مالی',N'Company',N'View',2),
(N'مدیر مالی',N'BaseItem',N'View',2),
(N'مدیر مالی',N'BaseDescription',N'View',2),
(N'کارشناس مالی',N'Company',N'View',2),
(N'کارشناس مالی',N'BaseItem',N'View',2),
(N'کارشناس مالی',N'BaseDescription',N'View',2),
(N'مدیر عامل',N'Company',N'View',2),
(N'مدیر عامل',N'BaseItem',N'View',2),
(N'مدیر عامل',N'BaseDescription',N'View',2),
(N'مدیر اجرایی',N'Company',N'View',2),
(N'مدیر اجرایی',N'BaseItem',N'View',2),
(N'مدیر اجرایی',N'BaseDescription',N'View',2),
(N'تنخواه دار',N'Company',N'View',2),
(N'تنخواه دار',N'BaseItem',N'View',2),
(N'تنخواه دار',N'BaseDescription',N'View',2),
(N'کارشناس کنترل کیفیت',N'Company',N'View',2),
(N'کارشناس کنترل کیفیت',N'BaseItem',N'View',2),
(N'کارشناس کنترل کیفیت',N'BaseDescription',N'View',2),
(N'مدیر فروش',N'BaseItem',N'Create',2),
(N'مدیر تامین',N'BaseItem',N'Create',2),
(N'مدیر مالی',N'BaseItem',N'Create',2);
IF @Apply=0
BEGIN
 SELECT * FROM @Entities;
 SELECT * FROM @Permissions;
 SELECT v.*,g.Scope AS ExistingScope FROM @Grants v LEFT JOIN dbo.AspNetRoles r ON r.Name=v.RoleName LEFT JOIN dbo.AccessEntities e ON e.[Key]=v.EntityKey LEFT JOIN dbo.AccessPermissions p ON p.AccessEntityId=e.Id AND p.[Key]=v.PermissionKey LEFT JOIN dbo.AccessRolePermissions g ON g.RoleId=r.Id AND g.AccessPermissionId=p.Id;
 SELECT v.*,r.Id AS ExistingRoleId FROM @Grants v LEFT JOIN dbo.AspNetRoles r ON r.Name=v.RoleName;
 SELECT ur.UserId, v.RoleName, v.EntityKey, v.PermissionKey, v.Scope AS ProposedRoleScope
 FROM @Grants v JOIN dbo.AspNetRoles r ON r.Name=v.RoleName JOIN dbo.AspNetUserRoles ur ON ur.RoleId=r.Id
 ORDER BY ur.UserId,v.EntityKey,v.PermissionKey;
 -- Show every existing grant and override, including roles other than Admin.
 SELECT r.Name AS RoleName,p.[Key] AS PermissionKey,rp.Scope
 FROM dbo.AccessRolePermissions rp JOIN dbo.AspNetRoles r ON r.Id=rp.RoleId
 JOIN dbo.AccessPermissions p ON p.Id=rp.AccessPermissionId
 JOIN dbo.AccessEntities e ON e.Id=p.AccessEntityId WHERE e.[Key] IN (SELECT EntityKey FROM @Entities);
 SELECT up.UserId,p.[Key] AS PermissionKey,up.IsAllowed,up.Scope
 FROM dbo.AccessUserPermissions up JOIN dbo.AccessPermissions p ON p.Id=up.AccessPermissionId
 JOIN dbo.AccessEntities e ON e.Id=p.AccessEntityId WHERE e.[Key] IN (SELECT EntityKey FROM @Entities);
 RETURN;
END;
BEGIN TRY
 BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE Name=N'مدیر ارشد')
 THROW 51083,'Required Admin role is missing.',1;
IF EXISTS (SELECT 1 FROM dbo.AccessEntities e JOIN @Entities v ON e.[Key]=v.EntityKey OR (e.DbContextName=N'CRMContextDb' AND e.ModelFullName=v.ModelFullName) WHERE e.[Key]<>v.EntityKey OR e.DbContextName<>N'CRMContextDb' OR e.ModelFullName<>v.ModelFullName)
 THROW 51081,'Entity key/model collision.',1;
INSERT INTO dbo.AccessEntities(Id,[Key],Title,[Description],DbContextName,ModelFullName,IsActive,CreatedAtUtc)
SELECT NEWID(),v.EntityKey,v.Title,v.Title,N'CRMContextDb',v.ModelFullName,1,SYSUTCDATETIME() FROM @Entities v
WHERE NOT EXISTS(SELECT 1 FROM dbo.AccessEntities e WHERE e.[Key]=v.EntityKey);
INSERT INTO dbo.AccessPermissions(Id,AccessEntityId,[Key],Title,[Description],IsActive,SupportsScope,CreatedAtUtc)
SELECT NEWID(),e.Id,v.PermissionKey,v.Title,v.Title,1,v.SupportsScope,SYSUTCDATETIME()
FROM @Permissions v JOIN dbo.AccessEntities e ON e.[Key]=v.EntityKey
WHERE NOT EXISTS(SELECT 1 FROM dbo.AccessPermissions p WHERE p.AccessEntityId=e.Id AND p.[Key]=v.PermissionKey);
INSERT INTO dbo.AccessRolePermissions(RoleId,AccessPermissionId,Scope)
SELECT r.Id,p.Id,v.Scope FROM @Grants v
JOIN dbo.AspNetRoles r ON r.Name=v.RoleName JOIN dbo.AccessEntities e ON e.[Key]=v.EntityKey AND e.IsActive=1
JOIN dbo.AccessPermissions p ON p.AccessEntityId=e.Id AND p.[Key]=v.PermissionKey AND p.IsActive=1
WHERE NOT EXISTS(SELECT 1 FROM dbo.AccessRolePermissions rp WHERE rp.RoleId=r.Id AND rp.AccessPermissionId=p.Id);
IF EXISTS (SELECT 1 FROM @Permissions v
 WHERE NOT EXISTS (SELECT 1 FROM dbo.AccessEntities e JOIN dbo.AccessPermissions p ON p.AccessEntityId=e.Id
 WHERE e.[Key]=v.EntityKey AND e.IsActive=1 AND p.[Key]=v.PermissionKey AND p.IsActive=1 AND p.SupportsScope=v.SupportsScope))
 THROW 51084,'Existing inactive or incompatible batch permission; review without overwriting.',1;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
