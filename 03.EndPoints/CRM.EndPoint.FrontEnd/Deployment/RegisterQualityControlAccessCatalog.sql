-- UserConnection only. Register the requested entities and legacy role grants; existing overrides are preserved.
-- Preview first; review all grants, especially private communications and shared configuration.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit=0;
IF OBJECT_ID(N'dbo.AccessRolePermissions',N'U') IS NULL OR COL_LENGTH(N'dbo.AccessRolePermissions',N'Scope') IS NULL THROW 51080,'Apply access catalog migrations first.',1;
DECLARE @Entities TABLE(EntityKey nvarchar(128) PRIMARY KEY, Title nvarchar(256), ModelFullName nvarchar(512));
INSERT INTO @Entities VALUES (N'QualityControl',N'کنترل کیفیت',N'CRM.Core.Domain.QualityControls.Entities.QualityControl'),
(N'QualityControlQuestion',N'سؤالات کنترل کیفیت',N'CRM.Core.Domain.QualityControls.Entities.QualityControlQuestion');
DECLARE @Permissions TABLE(EntityKey nvarchar(128), PermissionKey nvarchar(128), Title nvarchar(256), SupportsScope bit);
INSERT INTO @Permissions VALUES (N'QualityControl',N'View',N'مشاهده',0),
(N'QualityControl',N'Create',N'ایجاد',0),
(N'QualityControl',N'Edit',N'ویرایش',0),
(N'QualityControl',N'Delete',N'حذف',0),
(N'QualityControl',N'SetContactStatus',N'ثبت وضعیت تماس',0),
(N'QualityControl',N'ManageContactHistory',N'مدیریت تاریخچه تماس',0),
(N'QualityControlQuestion',N'View',N'مشاهده',0),
(N'QualityControlQuestion',N'Create',N'ایجاد',0),
(N'QualityControlQuestion',N'Edit',N'ویرایش',0),
(N'QualityControlQuestion',N'Delete',N'حذف',0);
DECLARE @Grants TABLE(RoleName nvarchar(256), EntityKey nvarchar(128), PermissionKey nvarchar(128), Scope tinyint);
INSERT INTO @Grants VALUES (N'مدیر ارشد',N'QualityControl',N'View',2),
(N'مدیر ارشد',N'QualityControl',N'Create',2),
(N'مدیر ارشد',N'QualityControl',N'Edit',2),
(N'مدیر ارشد',N'QualityControl',N'Delete',2),
(N'مدیر ارشد',N'QualityControl',N'SetContactStatus',2),
(N'مدیر ارشد',N'QualityControl',N'ManageContactHistory',2),
(N'مدیر ارشد',N'QualityControlQuestion',N'View',2),
(N'مدیر ارشد',N'QualityControlQuestion',N'Create',2),
(N'مدیر ارشد',N'QualityControlQuestion',N'Edit',2),
(N'مدیر ارشد',N'QualityControlQuestion',N'Delete',2),
(N'کارشناس کنترل کیفیت',N'QualityControl',N'View',2),
(N'کارشناس کنترل کیفیت',N'QualityControl',N'Create',2),
(N'کارشناس کنترل کیفیت',N'QualityControl',N'Edit',2),
(N'کارشناس کنترل کیفیت',N'QualityControl',N'Delete',2),
(N'کارشناس کنترل کیفیت',N'QualityControl',N'SetContactStatus',2),
(N'کارشناس کنترل کیفیت',N'QualityControlQuestion',N'View',2);
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
