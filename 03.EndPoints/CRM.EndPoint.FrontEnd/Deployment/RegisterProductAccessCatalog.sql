-- UserConnection only. Register the requested entities and legacy role grants; existing overrides are preserved.
-- Preview first; review all grants, especially private communications and shared configuration.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit=0;
IF OBJECT_ID(N'dbo.AccessRolePermissions',N'U') IS NULL OR COL_LENGTH(N'dbo.AccessRolePermissions',N'Scope') IS NULL THROW 51080,'Apply access catalog migrations first.',1;
DECLARE @Entities TABLE(EntityKey nvarchar(128) PRIMARY KEY, Title nvarchar(256), ModelFullName nvarchar(512));
INSERT INTO @Entities VALUES (N'Product',N'کالاها',N'CRM.Core.Domain.Products.Entities.Product'),
(N'ProductGroup',N'گروه کالاها',N'CRM.Core.Domain.Products.Entities.ProductGroup'),
(N'ProductAmount',N'قیمت کالاها',N'CRM.Core.Domain.Products.Entities.ProductAmount');
DECLARE @Permissions TABLE(EntityKey nvarchar(128), PermissionKey nvarchar(128), Title nvarchar(256), SupportsScope bit);
INSERT INTO @Permissions VALUES (N'Product',N'View',N'مشاهده',0),
(N'Product',N'Create',N'ایجاد',0),
(N'Product',N'Edit',N'ویرایش',0),
(N'Product',N'Delete',N'حذف',0),
(N'ProductGroup',N'View',N'مشاهده',0),
(N'ProductGroup',N'Create',N'ایجاد',0),
(N'ProductGroup',N'Edit',N'ویرایش',0),
(N'ProductGroup',N'Delete',N'حذف',0),
(N'ProductAmount',N'View',N'مشاهده',0),
(N'ProductAmount',N'Create',N'ایجاد',0),
(N'ProductAmount',N'Edit',N'ویرایش',0),
(N'ProductAmount',N'Delete',N'حذف',0);
DECLARE @Grants TABLE(RoleName nvarchar(256), EntityKey nvarchar(128), PermissionKey nvarchar(128), Scope tinyint);
INSERT INTO @Grants VALUES (N'مدیر ارشد',N'Product',N'View',2),
(N'مدیر ارشد',N'Product',N'Create',2),
(N'مدیر ارشد',N'Product',N'Edit',2),
(N'مدیر ارشد',N'Product',N'Delete',2),
(N'مدیر ارشد',N'ProductGroup',N'View',2),
(N'مدیر ارشد',N'ProductGroup',N'Create',2),
(N'مدیر ارشد',N'ProductGroup',N'Edit',2),
(N'مدیر ارشد',N'ProductGroup',N'Delete',2),
(N'مدیر ارشد',N'ProductAmount',N'View',2),
(N'مدیر ارشد',N'ProductAmount',N'Create',2),
(N'مدیر ارشد',N'ProductAmount',N'Edit',2),
(N'مدیر ارشد',N'ProductAmount',N'Delete',2),
(N'مدیر فروش',N'Product',N'View',2),
(N'مدیر فروش',N'ProductGroup',N'View',2),
(N'کارشناس فروش',N'Product',N'View',2),
(N'کارشناس فروش',N'ProductGroup',N'View',2),
(N'مدیر تامین',N'Product',N'View',2),
(N'مدیر تامین',N'ProductGroup',N'View',2),
(N'کارشناس تامین',N'Product',N'View',2),
(N'کارشناس تامین',N'ProductGroup',N'View',2),
(N'مدیر مالی',N'Product',N'View',2),
(N'مدیر مالی',N'ProductGroup',N'View',2),
(N'کارشناس مالی',N'Product',N'View',2),
(N'کارشناس مالی',N'ProductGroup',N'View',2),
(N'مدیر عامل',N'Product',N'View',2),
(N'مدیر عامل',N'ProductGroup',N'View',2),
(N'مدیر اجرایی',N'Product',N'View',2),
(N'مدیر اجرایی',N'ProductGroup',N'View',2),
(N'تنخواه دار',N'Product',N'View',2),
(N'تنخواه دار',N'ProductGroup',N'View',2),
(N'کارشناس کنترل کیفیت',N'Product',N'View',2),
(N'کارشناس کنترل کیفیت',N'ProductGroup',N'View',2),
(N'مدیر فروش',N'ProductAmount',N'View',2),
(N'کارشناس فروش',N'ProductAmount',N'View',2),
(N'مدیر تامین',N'ProductAmount',N'View',2),
(N'کارشناس تامین',N'ProductAmount',N'View',2),
(N'مدیر فروش',N'ProductAmount',N'Create',2),
(N'مدیر فروش',N'ProductAmount',N'Edit',2),
(N'مدیر فروش',N'ProductAmount',N'Delete',2),
(N'مدیر تامین',N'ProductAmount',N'Create',2),
(N'مدیر تامین',N'ProductAmount',N'Edit',2),
(N'مدیر تامین',N'ProductAmount',N'Delete',2),
(N'کارشناس تامین',N'ProductAmount',N'Create',2),
(N'کارشناس تامین',N'ProductAmount',N'Edit',2),
(N'کارشناس تامین',N'ProductAmount',N'Delete',2),
(N'مدیر فروش',N'Product',N'Create',2),
(N'مدیر تامین',N'Product',N'Create',2),
(N'مدیر مالی',N'Product',N'Create',2);
IF @Apply=0
BEGIN
 SELECT * FROM @Entities;
 SELECT * FROM @Permissions;
 SELECT v.*,r.Id AS ExistingRoleId FROM @Grants v LEFT JOIN dbo.AspNetRoles r ON r.Name=v.RoleName;
 SELECT ur.UserId, v.RoleName, v.EntityKey, v.PermissionKey, v.Scope AS ProposedRoleScope
 FROM @Grants v JOIN dbo.AspNetRoles r ON r.Name=v.RoleName JOIN dbo.AspNetUserRoles ur ON ur.RoleId=r.Id
 ORDER BY ur.UserId,v.EntityKey,v.PermissionKey;
 RETURN;
END;
BEGIN TRY
 BEGIN TRANSACTION;
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
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
