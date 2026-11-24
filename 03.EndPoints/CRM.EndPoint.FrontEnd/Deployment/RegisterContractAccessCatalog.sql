-- UserConnection only. Entity 29: Contract. Existing grants and explicit user overrides are preserved.
-- The previous staged catalog explicitly grants Admin only. The old endpoint was broader (NotCustomerOnly),
-- but no corresponding menu or approved role list was found. Do not silently expand sensitive contract access.
-- Preview first. Add any approved additional grants through the access administration UI.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit=0;
IF OBJECT_ID(N'dbo.AccessRolePermissions',N'U') IS NULL OR COL_LENGTH(N'dbo.AccessRolePermissions',N'Scope') IS NULL THROW 51080,'Apply access catalog migrations first.',1;
DECLARE @Entities TABLE(EntityKey nvarchar(128) PRIMARY KEY, Title nvarchar(256), ModelFullName nvarchar(512));
INSERT INTO @Entities VALUES (N'Contract',N'قراردادها',N'CRM.Core.Domain.Contracts.Entities.Contract');
DECLARE @Permissions TABLE(EntityKey nvarchar(128), PermissionKey nvarchar(128), Title nvarchar(256), SupportsScope bit, PRIMARY KEY(EntityKey,PermissionKey));
INSERT INTO @Permissions VALUES
(N'Contract',N'View',N'مشاهده',1),
(N'Contract',N'Create',N'ایجاد',0),
(N'Contract',N'Edit',N'ویرایش',1),
(N'Contract',N'Delete',N'حذف',1);
DECLARE @Grants TABLE(RoleName nvarchar(256), EntityKey nvarchar(128), PermissionKey nvarchar(128), Scope tinyint, PRIMARY KEY(RoleName,EntityKey,PermissionKey));
INSERT INTO @Grants VALUES
(N'مدیر ارشد',N'Contract',N'View',2),
(N'مدیر ارشد',N'Contract',N'Create',2),
(N'مدیر ارشد',N'Contract',N'Edit',2),
(N'مدیر ارشد',N'Contract',N'Delete',2);
IF @Apply=0
BEGIN
 SELECT * FROM @Entities;
 SELECT * FROM @Permissions;
 SELECT v.*,r.Id AS ExistingRoleId FROM @Grants v LEFT JOIN dbo.AspNetRoles r ON r.Name=v.RoleName;
 SELECT ur.UserId, v.RoleName, v.EntityKey, v.PermissionKey, v.Scope AS ProposedRoleScope
 FROM @Grants v JOIN dbo.AspNetRoles r ON r.Name=v.RoleName JOIN dbo.AspNetUserRoles ur ON ur.RoleId=r.Id
 ORDER BY ur.UserId,v.EntityKey,v.PermissionKey;
 -- Show every existing grant and override, including roles other than Admin.
 SELECT r.Name AS RoleName,p.[Key] AS PermissionKey,rp.Scope
 FROM dbo.AccessRolePermissions rp JOIN dbo.AspNetRoles r ON r.Id=rp.RoleId
 JOIN dbo.AccessPermissions p ON p.Id=rp.AccessPermissionId
 JOIN dbo.AccessEntities e ON e.Id=p.AccessEntityId WHERE e.[Key]=N'Contract';
 SELECT up.UserId,p.[Key] AS PermissionKey,up.IsAllowed,up.Scope
 FROM dbo.AccessUserPermissions up JOIN dbo.AccessPermissions p ON p.Id=up.AccessPermissionId
 JOIN dbo.AccessEntities e ON e.Id=p.AccessEntityId WHERE e.[Key]=N'Contract';
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
 THROW 51084,'Existing inactive or incompatible Contract permission; review without overwriting.',1;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
