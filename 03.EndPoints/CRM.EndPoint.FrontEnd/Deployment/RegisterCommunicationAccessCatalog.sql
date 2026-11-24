-- UserConnection only. Entities 30–35: Carizo, Report, CallRecording, Chat, Email, Contact.
-- Existing role grants, user overrides and scope memberships are preserved.
-- Role defaults: personal tasks; scoped reports/calls; legacy manager access to other reports/calls;
-- participant-only chat/email; admin-only Contact until an existing independent workflow is verified.
-- Scope 1 = Own, 2 = All. All for Chat/Email never bypasses participant ownership.
-- Existing role grants differing from these proposed defaults are shown, not overwritten.
-- Preview first. Apply only after reviewing differences and the branch CI result.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit=0;
IF OBJECT_ID(N'dbo.AccessEntities',N'U') IS NULL OR OBJECT_ID(N'dbo.AccessPermissions',N'U') IS NULL
 OR OBJECT_ID(N'dbo.AccessRolePermissions',N'U') IS NULL OR OBJECT_ID(N'dbo.AccessUserPermissions',N'U') IS NULL
 OR COL_LENGTH(N'dbo.AccessRolePermissions',N'Scope') IS NULL OR COL_LENGTH(N'dbo.AccessPermissions',N'SupportsScope') IS NULL
 THROW 51080,'Apply access catalog schema migrations in UserConnection first.',1;
DECLARE @Entities TABLE(EntityKey nvarchar(128) PRIMARY KEY, Title nvarchar(256), ModelFullName nvarchar(512));
INSERT INTO @Entities VALUES
(N'Carizo',N'کاریز',N'CRM.Core.Domain.Carizos.Entities.Carizo'),
(N'Report',N'گزارش‌ها',N'CRM.Core.Domain.Reports.Entities.Report'),
(N'CallRecording',N'مکالمات تلفنی',N'CRM.Core.Domain.CallRecordings.Entities.CallRecording'),
(N'Chat',N'گفتگوها',N'CRM.Core.Domain.Chats.Entities.Chat'),
(N'Email',N'ایمیل‌ها',N'CRM.Core.Domain.Emails.Entities.Email'),
(N'Contact',N'مخاطبین',N'CRM.Core.Domain.Contacts.Entities.Contact');
DECLARE @Permissions TABLE(EntityKey nvarchar(128), PermissionKey nvarchar(128), Title nvarchar(256), SupportsScope bit, PRIMARY KEY(EntityKey,PermissionKey));
INSERT INTO @Permissions VALUES
(N'Carizo',N'View',N'مشاهده',1),
(N'Carizo',N'Create',N'ایجاد',0),
(N'Carizo',N'Edit',N'ویرایش',1),
(N'Carizo',N'Delete',N'حذف',1),
(N'Carizo',N'ChangeStatus',N'تغییر وضعیت',1),
(N'Report',N'View',N'مشاهده',1),
(N'Report',N'Create',N'ایجاد',0),
(N'Report',N'Edit',N'ویرایش',1),
(N'Report',N'Delete',N'حذف',1),
(N'Report',N'Export',N'خروجی',1),
(N'CallRecording',N'View',N'مشاهده',1),
(N'CallRecording',N'Listen',N'شنیدن مکالمه',1),
(N'Chat',N'View',N'مشاهده',0),
(N'Chat',N'Create',N'ایجاد',0),
(N'Chat',N'Edit',N'ویرایش',0),
(N'Chat',N'Delete',N'حذف',0),
(N'Email',N'View',N'مشاهده',0),
(N'Email',N'MarkRead',N'ثبت مشاهده',0),
(N'Contact',N'View',N'مشاهده',1),
(N'Contact',N'Create',N'ایجاد',0),
(N'Contact',N'Edit',N'ویرایش',1),
(N'Contact',N'Delete',N'حذف',1);
DECLARE @Grants TABLE(RoleName nvarchar(256), EntityKey nvarchar(128), PermissionKey nvarchar(128), Scope tinyint, PRIMARY KEY(RoleName,EntityKey,PermissionKey));
INSERT INTO @Grants VALUES
(N'مدیر ارشد',N'Carizo',N'View',2),
(N'مدیر ارشد',N'Carizo',N'Create',2),
(N'مدیر ارشد',N'Carizo',N'Edit',2),
(N'مدیر ارشد',N'Carizo',N'Delete',2),
(N'مدیر ارشد',N'Carizo',N'ChangeStatus',2),
(N'مدیر ارشد',N'Report',N'View',2),
(N'مدیر ارشد',N'Report',N'Create',2),
(N'مدیر ارشد',N'Report',N'Edit',2),
(N'مدیر ارشد',N'Report',N'Delete',2),
(N'مدیر ارشد',N'Report',N'Export',2),
(N'مدیر ارشد',N'CallRecording',N'View',2),
(N'مدیر ارشد',N'CallRecording',N'Listen',2),
(N'مدیر ارشد',N'Chat',N'View',2),
(N'مدیر ارشد',N'Chat',N'Create',2),
(N'مدیر ارشد',N'Chat',N'Edit',2),
(N'مدیر ارشد',N'Chat',N'Delete',2),
(N'مدیر ارشد',N'Email',N'View',2),
(N'مدیر ارشد',N'Email',N'MarkRead',2),
(N'مدیر ارشد',N'Contact',N'View',2),
(N'مدیر ارشد',N'Contact',N'Create',2),
(N'مدیر ارشد',N'Contact',N'Edit',2),
(N'مدیر ارشد',N'Contact',N'Delete',2),
(N'مدیر فروش',N'Chat',N'View',2),
(N'مدیر فروش',N'Chat',N'Create',2),
(N'مدیر فروش',N'Chat',N'Edit',2),
(N'مدیر فروش',N'Chat',N'Delete',2),
(N'مدیر فروش',N'Email',N'View',2),
(N'مدیر فروش',N'Email',N'MarkRead',2),
(N'مدیر فروش',N'Carizo',N'View',1),
(N'مدیر فروش',N'Carizo',N'Create',2),
(N'مدیر فروش',N'Carizo',N'Edit',1),
(N'مدیر فروش',N'Carizo',N'Delete',1),
(N'مدیر فروش',N'Carizo',N'ChangeStatus',1),
(N'مدیر فروش',N'Report',N'View',2),
(N'مدیر فروش',N'Report',N'Create',2),
(N'مدیر فروش',N'Report',N'Edit',2),
(N'مدیر فروش',N'Report',N'Delete',2),
(N'مدیر فروش',N'Report',N'Export',2),
(N'مدیر فروش',N'CallRecording',N'View',2),
(N'مدیر فروش',N'CallRecording',N'Listen',2),
(N'کارشناس فروش',N'Chat',N'View',2),
(N'کارشناس فروش',N'Chat',N'Create',2),
(N'کارشناس فروش',N'Chat',N'Edit',2),
(N'کارشناس فروش',N'Chat',N'Delete',2),
(N'کارشناس فروش',N'Email',N'View',2),
(N'کارشناس فروش',N'Email',N'MarkRead',2),
(N'کارشناس فروش',N'Carizo',N'View',1),
(N'کارشناس فروش',N'Carizo',N'Create',2),
(N'کارشناس فروش',N'Carizo',N'Edit',1),
(N'کارشناس فروش',N'Carizo',N'Delete',1),
(N'کارشناس فروش',N'Carizo',N'ChangeStatus',1),
(N'کارشناس فروش',N'Report',N'View',1),
(N'کارشناس فروش',N'Report',N'Create',2),
(N'کارشناس فروش',N'Report',N'Edit',1),
(N'کارشناس فروش',N'Report',N'Delete',1),
(N'کارشناس فروش',N'Report',N'Export',1),
(N'کارشناس فروش',N'CallRecording',N'View',1),
(N'کارشناس فروش',N'CallRecording',N'Listen',1),
(N'مدیر تامین',N'Chat',N'View',2),
(N'مدیر تامین',N'Chat',N'Create',2),
(N'مدیر تامین',N'Chat',N'Edit',2),
(N'مدیر تامین',N'Chat',N'Delete',2),
(N'مدیر تامین',N'Email',N'View',2),
(N'مدیر تامین',N'Email',N'MarkRead',2),
(N'مدیر تامین',N'Carizo',N'View',1),
(N'مدیر تامین',N'Carizo',N'Create',2),
(N'مدیر تامین',N'Carizo',N'Edit',1),
(N'مدیر تامین',N'Carizo',N'Delete',1),
(N'مدیر تامین',N'Carizo',N'ChangeStatus',1),
(N'مدیر تامین',N'Report',N'View',1),
(N'مدیر تامین',N'Report',N'Create',2),
(N'مدیر تامین',N'Report',N'Edit',1),
(N'مدیر تامین',N'Report',N'Delete',1),
(N'مدیر تامین',N'Report',N'Export',1),
(N'مدیر تامین',N'CallRecording',N'View',1),
(N'مدیر تامین',N'CallRecording',N'Listen',1),
(N'کارشناس تامین',N'Chat',N'View',2),
(N'کارشناس تامین',N'Chat',N'Create',2),
(N'کارشناس تامین',N'Chat',N'Edit',2),
(N'کارشناس تامین',N'Chat',N'Delete',2),
(N'کارشناس تامین',N'Email',N'View',2),
(N'کارشناس تامین',N'Email',N'MarkRead',2),
(N'کارشناس تامین',N'Carizo',N'View',1),
(N'کارشناس تامین',N'Carizo',N'Create',2),
(N'کارشناس تامین',N'Carizo',N'Edit',1),
(N'کارشناس تامین',N'Carizo',N'Delete',1),
(N'کارشناس تامین',N'Carizo',N'ChangeStatus',1),
(N'کارشناس تامین',N'Report',N'View',1),
(N'کارشناس تامین',N'Report',N'Create',2),
(N'کارشناس تامین',N'Report',N'Edit',1),
(N'کارشناس تامین',N'Report',N'Delete',1),
(N'کارشناس تامین',N'Report',N'Export',1),
(N'کارشناس تامین',N'CallRecording',N'View',1),
(N'کارشناس تامین',N'CallRecording',N'Listen',1),
(N'مدیر مالی',N'Chat',N'View',2),
(N'مدیر مالی',N'Chat',N'Create',2),
(N'مدیر مالی',N'Chat',N'Edit',2),
(N'مدیر مالی',N'Chat',N'Delete',2),
(N'مدیر مالی',N'Email',N'View',2),
(N'مدیر مالی',N'Email',N'MarkRead',2),
(N'مدیر مالی',N'Carizo',N'View',1),
(N'مدیر مالی',N'Carizo',N'Create',2),
(N'مدیر مالی',N'Carizo',N'Edit',1),
(N'مدیر مالی',N'Carizo',N'Delete',1),
(N'مدیر مالی',N'Carizo',N'ChangeStatus',1),
(N'مدیر مالی',N'Report',N'View',1),
(N'مدیر مالی',N'Report',N'Create',2),
(N'مدیر مالی',N'Report',N'Edit',1),
(N'مدیر مالی',N'Report',N'Delete',1),
(N'مدیر مالی',N'Report',N'Export',1),
(N'مدیر مالی',N'CallRecording',N'View',1),
(N'مدیر مالی',N'CallRecording',N'Listen',1),
(N'کارشناس مالی',N'Chat',N'View',2),
(N'کارشناس مالی',N'Chat',N'Create',2),
(N'کارشناس مالی',N'Chat',N'Edit',2),
(N'کارشناس مالی',N'Chat',N'Delete',2),
(N'کارشناس مالی',N'Email',N'View',2),
(N'کارشناس مالی',N'Email',N'MarkRead',2),
(N'کارشناس مالی',N'Carizo',N'View',1),
(N'کارشناس مالی',N'Carizo',N'Create',2),
(N'کارشناس مالی',N'Carizo',N'Edit',1),
(N'کارشناس مالی',N'Carizo',N'Delete',1),
(N'کارشناس مالی',N'Carizo',N'ChangeStatus',1),
(N'کارشناس مالی',N'Report',N'View',1),
(N'کارشناس مالی',N'Report',N'Create',2),
(N'کارشناس مالی',N'Report',N'Edit',1),
(N'کارشناس مالی',N'Report',N'Delete',1),
(N'کارشناس مالی',N'Report',N'Export',1),
(N'کارشناس مالی',N'CallRecording',N'View',1),
(N'کارشناس مالی',N'CallRecording',N'Listen',1),
(N'مدیر عامل',N'Chat',N'View',2),
(N'مدیر عامل',N'Chat',N'Create',2),
(N'مدیر عامل',N'Chat',N'Edit',2),
(N'مدیر عامل',N'Chat',N'Delete',2),
(N'مدیر عامل',N'Email',N'View',2),
(N'مدیر عامل',N'Email',N'MarkRead',2),
(N'مدیر عامل',N'Carizo',N'View',1),
(N'مدیر عامل',N'Carizo',N'Create',2),
(N'مدیر عامل',N'Carizo',N'Edit',1),
(N'مدیر عامل',N'Carizo',N'Delete',1),
(N'مدیر عامل',N'Carizo',N'ChangeStatus',1),
(N'مدیر عامل',N'Report',N'View',2),
(N'مدیر عامل',N'Report',N'Create',2),
(N'مدیر عامل',N'Report',N'Edit',1),
(N'مدیر عامل',N'Report',N'Delete',1),
(N'مدیر عامل',N'Report',N'Export',2),
(N'مدیر عامل',N'CallRecording',N'View',2),
(N'مدیر عامل',N'CallRecording',N'Listen',2),
(N'مدیر اجرایی',N'Chat',N'View',2),
(N'مدیر اجرایی',N'Chat',N'Create',2),
(N'مدیر اجرایی',N'Chat',N'Edit',2),
(N'مدیر اجرایی',N'Chat',N'Delete',2),
(N'مدیر اجرایی',N'Email',N'View',2),
(N'مدیر اجرایی',N'Email',N'MarkRead',2),
(N'مدیر اجرایی',N'Carizo',N'View',1),
(N'مدیر اجرایی',N'Carizo',N'Create',2),
(N'مدیر اجرایی',N'Carizo',N'Edit',1),
(N'مدیر اجرایی',N'Carizo',N'Delete',1),
(N'مدیر اجرایی',N'Carizo',N'ChangeStatus',1),
(N'مدیر اجرایی',N'Report',N'View',2),
(N'مدیر اجرایی',N'Report',N'Create',2),
(N'مدیر اجرایی',N'Report',N'Edit',1),
(N'مدیر اجرایی',N'Report',N'Delete',1),
(N'مدیر اجرایی',N'Report',N'Export',2),
(N'مدیر اجرایی',N'CallRecording',N'View',2),
(N'مدیر اجرایی',N'CallRecording',N'Listen',2),
(N'تنخواه دار',N'Chat',N'View',2),
(N'تنخواه دار',N'Chat',N'Create',2),
(N'تنخواه دار',N'Chat',N'Edit',2),
(N'تنخواه دار',N'Chat',N'Delete',2),
(N'تنخواه دار',N'Email',N'View',2),
(N'تنخواه دار',N'Email',N'MarkRead',2),
(N'تنخواه دار',N'Carizo',N'View',1),
(N'تنخواه دار',N'Carizo',N'Create',2),
(N'تنخواه دار',N'Carizo',N'Edit',1),
(N'تنخواه دار',N'Carizo',N'Delete',1),
(N'تنخواه دار',N'Carizo',N'ChangeStatus',1),
(N'تنخواه دار',N'Report',N'View',1),
(N'تنخواه دار',N'Report',N'Create',2),
(N'تنخواه دار',N'Report',N'Edit',1),
(N'تنخواه دار',N'Report',N'Delete',1),
(N'تنخواه دار',N'Report',N'Export',1),
(N'تنخواه دار',N'CallRecording',N'View',1),
(N'تنخواه دار',N'CallRecording',N'Listen',1),
(N'کارشناس کنترل کیفیت',N'Chat',N'View',2),
(N'کارشناس کنترل کیفیت',N'Chat',N'Create',2),
(N'کارشناس کنترل کیفیت',N'Chat',N'Edit',2),
(N'کارشناس کنترل کیفیت',N'Chat',N'Delete',2),
(N'کارشناس کنترل کیفیت',N'Email',N'View',2),
(N'کارشناس کنترل کیفیت',N'Email',N'MarkRead',2),
(N'کارشناس کنترل کیفیت',N'Carizo',N'View',1),
(N'کارشناس کنترل کیفیت',N'Carizo',N'Create',2),
(N'کارشناس کنترل کیفیت',N'Carizo',N'Edit',1),
(N'کارشناس کنترل کیفیت',N'Carizo',N'Delete',1),
(N'کارشناس کنترل کیفیت',N'Carizo',N'ChangeStatus',1),
(N'کارشناس کنترل کیفیت',N'Report',N'View',1),
(N'کارشناس کنترل کیفیت',N'Report',N'Create',2),
(N'کارشناس کنترل کیفیت',N'Report',N'Edit',1),
(N'کارشناس کنترل کیفیت',N'Report',N'Delete',1),
(N'کارشناس کنترل کیفیت',N'Report',N'Export',1),
(N'کارشناس کنترل کیفیت',N'CallRecording',N'View',1),
(N'کارشناس کنترل کیفیت',N'CallRecording',N'Listen',1),
(N'مشتری',N'Chat',N'View',2),
(N'مشتری',N'Chat',N'Create',2),
(N'مشتری',N'Chat',N'Edit',2),
(N'مشتری',N'Chat',N'Delete',2),
(N'مشتری',N'Email',N'View',2),
(N'مشتری',N'Email',N'MarkRead',2),
(N'تامین کننده',N'Chat',N'View',2),
(N'تامین کننده',N'Chat',N'Create',2),
(N'تامین کننده',N'Chat',N'Edit',2),
(N'تامین کننده',N'Chat',N'Delete',2),
(N'تامین کننده',N'Email',N'View',2),
(N'تامین کننده',N'Email',N'MarkRead',2);
-- Preview current catalog, proposed grants, affected users and explicit overrides.
SELECT v.*,e.Id AS ExistingEntityId,e.IsActive FROM @Entities v LEFT JOIN dbo.AccessEntities e ON e.[Key]=v.EntityKey;
SELECT v.*,p.Id AS ExistingPermissionId,p.IsActive,p.SupportsScope AS ExistingSupportsScope
 FROM @Permissions v LEFT JOIN dbo.AccessEntities e ON e.[Key]=v.EntityKey
 LEFT JOIN dbo.AccessPermissions p ON p.AccessEntityId=e.Id AND p.[Key]=v.PermissionKey;
SELECT v.*,r.Id AS ExistingRoleId,rp.Scope AS ExistingScope,
 CASE WHEN r.Id IS NULL THEN N'MissingRole' WHEN rp.RoleId IS NULL THEN N'Insert'
 WHEN rp.Scope<>v.Scope THEN N'PreserveExistingDifferentScope' ELSE N'AlreadyExists' END AS PlannedAction
 FROM @Grants v LEFT JOIN dbo.AspNetRoles r ON r.Name=v.RoleName
 LEFT JOIN dbo.AccessEntities e ON e.[Key]=v.EntityKey
 LEFT JOIN dbo.AccessPermissions p ON p.AccessEntityId=e.Id AND p.[Key]=v.PermissionKey
 LEFT JOIN dbo.AccessRolePermissions rp ON rp.RoleId=r.Id AND rp.AccessPermissionId=p.Id;
SELECT ur.UserId,u.UserName,v.RoleName,v.EntityKey,v.PermissionKey,v.Scope AS ProposedRoleScope
 FROM @Grants v JOIN dbo.AspNetRoles r ON r.Name=v.RoleName JOIN dbo.AspNetUserRoles ur ON ur.RoleId=r.Id
 JOIN dbo.AspNetUsers u ON u.Id=ur.UserId ORDER BY u.UserName,v.EntityKey,v.PermissionKey;
SELECT up.UserId,e.[Key] AS EntityKey,p.[Key] AS PermissionKey,up.IsAllowed,up.Scope
 FROM dbo.AccessUserPermissions up JOIN dbo.AccessPermissions p ON p.Id=up.AccessPermissionId
 JOIN dbo.AccessEntities e ON e.Id=p.AccessEntityId JOIN @Entities v ON v.EntityKey=e.[Key];
IF @Apply=0 RETURN;
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
 THROW 51084,'Existing inactive or incompatible permission; review without overwriting.',1;
IF EXISTS (SELECT 1 FROM @Grants v JOIN dbo.AspNetRoles r ON r.Name=v.RoleName
 JOIN dbo.AccessEntities e ON e.[Key]=v.EntityKey JOIN dbo.AccessPermissions p ON p.AccessEntityId=e.Id AND p.[Key]=v.PermissionKey
 WHERE NOT EXISTS (SELECT 1 FROM dbo.AccessRolePermissions rp WHERE rp.RoleId=r.Id AND rp.AccessPermissionId=p.Id))
 THROW 51085,'An expected role grant was not registered.',1;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;

-- Post-apply evidence: only report current rows; actual user scope and SQL/browser tests are separate.
SELECT e.[Key] AS EntityKey,p.[Key] AS PermissionKey,p.SupportsScope,r.Name AS RoleName,rp.Scope
FROM dbo.AccessEntities e JOIN @Entities v ON v.EntityKey=e.[Key]
JOIN dbo.AccessPermissions p ON p.AccessEntityId=e.Id
LEFT JOIN dbo.AccessRolePermissions rp ON rp.AccessPermissionId=p.Id
LEFT JOIN dbo.AspNetRoles r ON r.Id=rp.RoleId
ORDER BY e.[Key],p.[Key],r.Name;
