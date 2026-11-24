-- UserConnection only. Entity 48: virtual Dashboard; no CRM table or migration.
-- Preview-first, preserves existing grants and explicit user overrides.
-- Initial grants follow the previous nine navigation roles and legacy aggregate owner limits.
-- The shared Report/Carizo source scopes still apply. Logs are initially Admin only and require LogSystem enforcement.
-- Non-menu roles previously able to navigate directly may need individual Dashboard grants.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit=0;
IF OBJECT_ID(N'dbo.AccessRolePermissions',N'U') IS NULL OR COL_LENGTH(N'dbo.AccessRolePermissions',N'Scope') IS NULL THROW 51080,'Apply access catalog migrations first.',1;
DECLARE @Entities TABLE(EntityKey nvarchar(128) PRIMARY KEY, Title nvarchar(256), ModelFullName nvarchar(512));
INSERT INTO @Entities VALUES (N'Dashboard',N'داشبورد',N'CRM.Core.Domain.AccessCatalog.Modules.Dashboard');
DECLARE @Permissions TABLE(EntityKey nvarchar(128), PermissionKey nvarchar(128), Title nvarchar(256), SupportsScope bit, PRIMARY KEY(EntityKey,PermissionKey));
INSERT INTO @Permissions VALUES
(N'Dashboard',N'View',N'مشاهده داشبورد',0),
(N'Dashboard',N'ViewSales',N'مشاهده میزان فروش',1),
(N'Dashboard',N'ViewSalesDetails',N'مشاهده فروش به تفکیک کارشناسان',1),
(N'Dashboard',N'ViewOrders',N'مشاهده آمار دستور تولید',1),
(N'Dashboard',N'ViewCustomers',N'مشاهده آمار مشتریان فعال',1),
(N'Dashboard',N'ViewReportCount',N'مشاهده تعداد گزارش‌ها',1),
(N'Dashboard',N'ViewReports',N'مشاهده آخرین گزارش‌ها',0),
(N'Dashboard',N'ViewBankBalance',N'مشاهده موجودی بانک',0),
(N'Dashboard',N'ViewReceivables',N'مشاهده مطالبات',0),
(N'Dashboard',N'ViewPayables',N'مشاهده بدهی‌ها',0),
(N'Dashboard',N'ViewTasks',N'مشاهده فعالیت‌های امروز',0),
(N'Dashboard',N'ViewLogs',N'مشاهده تاریخچه عملکرد کاربران',0),
(N'Dashboard',N'ViewLogDetails',N'مشاهده جزئیات عملکرد کاربران',0);
DECLARE @Grants TABLE(RoleName nvarchar(256), EntityKey nvarchar(128), PermissionKey nvarchar(128), Scope tinyint, PRIMARY KEY(RoleName,EntityKey,PermissionKey));
INSERT INTO @Grants VALUES
(N'مدیر ارشد',N'Dashboard',N'View',2),
(N'مدیر ارشد',N'Dashboard',N'ViewReports',2),
(N'مدیر ارشد',N'Dashboard',N'ViewTasks',2),
(N'مدیر ارشد',N'Dashboard',N'ViewSales',2),
(N'مدیر ارشد',N'Dashboard',N'ViewOrders',2),
(N'مدیر ارشد',N'Dashboard',N'ViewCustomers',2),
(N'مدیر ارشد',N'Dashboard',N'ViewReportCount',2),
(N'مدیر ارشد',N'Dashboard',N'ViewSalesDetails',2),
(N'مدیر ارشد',N'Dashboard',N'ViewBankBalance',2),
(N'مدیر ارشد',N'Dashboard',N'ViewReceivables',2),
(N'مدیر ارشد',N'Dashboard',N'ViewPayables',2),
(N'مدیر ارشد',N'Dashboard',N'ViewLogs',2),
(N'مدیر ارشد',N'Dashboard',N'ViewLogDetails',2),
(N'مدیر عامل',N'Dashboard',N'View',2),
(N'مدیر عامل',N'Dashboard',N'ViewReports',2),
(N'مدیر عامل',N'Dashboard',N'ViewTasks',2),
(N'مدیر عامل',N'Dashboard',N'ViewSales',1),
(N'مدیر عامل',N'Dashboard',N'ViewOrders',1),
(N'مدیر عامل',N'Dashboard',N'ViewCustomers',1),
(N'مدیر عامل',N'Dashboard',N'ViewReportCount',1),
(N'مدیر عامل',N'Dashboard',N'ViewSalesDetails',2),
(N'مدیر عامل',N'Dashboard',N'ViewBankBalance',2),
(N'مدیر عامل',N'Dashboard',N'ViewReceivables',2),
(N'مدیر عامل',N'Dashboard',N'ViewPayables',2),
(N'مدیر اجرایی',N'Dashboard',N'View',2),
(N'مدیر اجرایی',N'Dashboard',N'ViewReports',2),
(N'مدیر اجرایی',N'Dashboard',N'ViewTasks',2),
(N'مدیر اجرایی',N'Dashboard',N'ViewSales',1),
(N'مدیر اجرایی',N'Dashboard',N'ViewOrders',1),
(N'مدیر اجرایی',N'Dashboard',N'ViewCustomers',1),
(N'مدیر اجرایی',N'Dashboard',N'ViewReportCount',1),
(N'کارشناس فروش',N'Dashboard',N'View',2),
(N'کارشناس فروش',N'Dashboard',N'ViewReports',2),
(N'کارشناس فروش',N'Dashboard',N'ViewTasks',2),
(N'کارشناس فروش',N'Dashboard',N'ViewSales',1),
(N'کارشناس فروش',N'Dashboard',N'ViewOrders',1),
(N'کارشناس فروش',N'Dashboard',N'ViewCustomers',1),
(N'کارشناس فروش',N'Dashboard',N'ViewReportCount',1),
(N'مدیر فروش',N'Dashboard',N'View',2),
(N'مدیر فروش',N'Dashboard',N'ViewReports',2),
(N'مدیر فروش',N'Dashboard',N'ViewTasks',2),
(N'مدیر فروش',N'Dashboard',N'ViewSales',2),
(N'مدیر فروش',N'Dashboard',N'ViewOrders',2),
(N'مدیر فروش',N'Dashboard',N'ViewCustomers',2),
(N'مدیر فروش',N'Dashboard',N'ViewReportCount',2),
(N'مدیر فروش',N'Dashboard',N'ViewSalesDetails',2),
(N'کارشناس تامین',N'Dashboard',N'View',2),
(N'کارشناس تامین',N'Dashboard',N'ViewReports',2),
(N'کارشناس تامین',N'Dashboard',N'ViewTasks',2),
(N'کارشناس تامین',N'Dashboard',N'ViewSales',1),
(N'کارشناس تامین',N'Dashboard',N'ViewOrders',1),
(N'کارشناس تامین',N'Dashboard',N'ViewCustomers',1),
(N'کارشناس تامین',N'Dashboard',N'ViewReportCount',1),
(N'مدیر تامین',N'Dashboard',N'View',2),
(N'مدیر تامین',N'Dashboard',N'ViewReports',2),
(N'مدیر تامین',N'Dashboard',N'ViewTasks',2),
(N'مدیر تامین',N'Dashboard',N'ViewSales',1),
(N'مدیر تامین',N'Dashboard',N'ViewOrders',1),
(N'مدیر تامین',N'Dashboard',N'ViewCustomers',1),
(N'مدیر تامین',N'Dashboard',N'ViewReportCount',1),
(N'مدیر تامین',N'Dashboard',N'ViewSalesDetails',2),
(N'کارشناس مالی',N'Dashboard',N'View',2),
(N'کارشناس مالی',N'Dashboard',N'ViewReports',2),
(N'کارشناس مالی',N'Dashboard',N'ViewTasks',2),
(N'کارشناس مالی',N'Dashboard',N'ViewSales',2),
(N'کارشناس مالی',N'Dashboard',N'ViewOrders',2),
(N'کارشناس مالی',N'Dashboard',N'ViewCustomers',2),
(N'کارشناس مالی',N'Dashboard',N'ViewReportCount',2),
(N'کارشناس مالی',N'Dashboard',N'ViewSalesDetails',2),
(N'کارشناس مالی',N'Dashboard',N'ViewBankBalance',2),
(N'کارشناس مالی',N'Dashboard',N'ViewReceivables',2),
(N'کارشناس مالی',N'Dashboard',N'ViewPayables',2),
(N'مدیر مالی',N'Dashboard',N'View',2),
(N'مدیر مالی',N'Dashboard',N'ViewReports',2),
(N'مدیر مالی',N'Dashboard',N'ViewTasks',2),
(N'مدیر مالی',N'Dashboard',N'ViewSales',2),
(N'مدیر مالی',N'Dashboard',N'ViewOrders',2),
(N'مدیر مالی',N'Dashboard',N'ViewCustomers',2),
(N'مدیر مالی',N'Dashboard',N'ViewReportCount',2),
(N'مدیر مالی',N'Dashboard',N'ViewSalesDetails',2),
(N'مدیر مالی',N'Dashboard',N'ViewBankBalance',2),
(N'مدیر مالی',N'Dashboard',N'ViewReceivables',2),
(N'مدیر مالی',N'Dashboard',N'ViewPayables',2);
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
 JOIN dbo.AccessEntities e ON e.Id=p.AccessEntityId WHERE e.[Key]=N'Dashboard';
 SELECT up.UserId,p.[Key] AS PermissionKey,up.IsAllowed,up.Scope
 FROM dbo.AccessUserPermissions up JOIN dbo.AccessPermissions p ON p.Id=up.AccessPermissionId
 JOIN dbo.AccessEntities e ON e.Id=p.AccessEntityId WHERE e.[Key]=N'Dashboard';
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
 THROW 51084,'Existing inactive or incompatible Dashboard permission; review without overwriting.',1;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
