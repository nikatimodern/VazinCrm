-- UserConnection database. Read-only verification after applying the two scoped registration scripts.
-- Scope codes: 0 denied, 1 own, 2 all, 3 selected owners, 4 all except selected owners.
-- Explicit user override supersedes role grants. Parent owner membership and real row visibility
-- still require application-level integration checks; this report does not modify grants.
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.AccessUserPermissions', N'U') IS NULL
    THROW 51082, 'Use UserConnection and apply access catalog migrations first.', 1;

DECLARE @Keys TABLE(EntityKey nvarchar(128), PermissionKey nvarchar(128), PRIMARY KEY(EntityKey, PermissionKey));
INSERT INTO @Keys VALUES
(N'QualityControl',N'View'),(N'QualityControl',N'Create'),(N'QualityControl',N'Edit'),
(N'QualityControl',N'Delete'),(N'QualityControl',N'SetContactStatus'),(N'QualityControl',N'ManageContactHistory'),
(N'QualityControlQuestion',N'View'),(N'QualityControlQuestion',N'Create'),(N'QualityControlQuestion',N'Edit'),(N'QualityControlQuestion',N'Delete'),
(N'Product',N'View'),(N'Product',N'Create'),(N'Product',N'Edit'),(N'Product',N'Delete'),
(N'ProductGroup',N'View'),(N'ProductGroup',N'Create'),(N'ProductGroup',N'Edit'),(N'ProductGroup',N'Delete'),
(N'ProductAmount',N'View'),(N'ProductAmount',N'Create'),(N'ProductAmount',N'Edit'),(N'ProductAmount',N'Delete'),
(N'Customer',N'View'),(N'Deal',N'View'),(N'Factor',N'View');

-- First result: missing or inactive catalog records, independent of users.
SELECT k.EntityKey, k.PermissionKey, e.IsActive AS EntityActive, p.IsActive AS PermissionActive, p.SupportsScope
FROM @Keys k
LEFT JOIN dbo.AccessEntities e ON e.[Key]=k.EntityKey
LEFT JOIN dbo.AccessPermissions p ON p.AccessEntityId=e.Id AND p.[Key]=k.PermissionKey
ORDER BY k.EntityKey,k.PermissionKey;

DECLARE @Effective TABLE(UserId nvarchar(450), EntityKey nvarchar(128), PermissionKey nvarchar(128),
    EffectiveScope tinyint, Source nvarchar(32), PRIMARY KEY(UserId,EntityKey,PermissionKey));
INSERT INTO @Effective
SELECT u.Id,k.EntityKey,k.PermissionKey,
    CASE WHEN p.Id IS NULL THEN 0
         WHEN custom.UserId IS NOT NULL THEN
            CASE WHEN custom.IsAllowed=0 THEN 0
                 WHEN custom.Scope=2 THEN 2
                 WHEN p.SupportsScope=1 AND custom.Scope=1 THEN 1
                 WHEN p.SupportsScope=1 AND k.EntityKey IN(N'Customer',N'Deal',N'Factor') AND custom.Scope IN(3,4)
                      THEN custom.Scope
                 ELSE 0 END
         ELSE COALESCE(roleGrant.Scope,0) END,
    CASE WHEN p.Id IS NULL THEN N'MissingOrInactive'
         WHEN custom.UserId IS NOT NULL THEN N'UserOverride'
         WHEN roleGrant.Scope IS NOT NULL THEN N'Role'
         ELSE N'NoGrant' END
FROM dbo.AspNetUsers u CROSS JOIN @Keys k
LEFT JOIN dbo.AccessEntities e ON e.[Key]=k.EntityKey AND e.IsActive=1
LEFT JOIN dbo.AccessPermissions p ON p.AccessEntityId=e.Id AND p.[Key]=k.PermissionKey AND p.IsActive=1
LEFT JOIN dbo.AccessUserPermissions custom ON custom.UserId=u.Id AND custom.AccessPermissionId=p.Id
OUTER APPLY (
    SELECT MAX(rp.Scope) AS Scope
    FROM dbo.AspNetUserRoles ur JOIN dbo.AccessRolePermissions rp ON rp.RoleId=ur.RoleId
    WHERE ur.UserId=u.Id AND rp.AccessPermissionId=p.Id
      AND (rp.Scope=2 OR (rp.Scope=1 AND p.SupportsScope=1))
) roleGrant;

-- Second result: configured effective permission scopes for each existing user.
SELECT u.UserName, a.UserId,a.EntityKey,a.PermissionKey,a.EffectiveScope,a.Source
FROM @Effective a JOIN dbo.AspNetUsers u ON u.Id=a.UserId
ORDER BY u.UserName,a.EntityKey,a.PermissionKey;

-- Third result: users with QC.View whose required read grants are absent.
SELECT u.UserName,q.UserId,dep.EntityKey,dep.EffectiveScope,dep.Source
FROM @Effective q JOIN dbo.AspNetUsers u ON u.Id=q.UserId
JOIN @Effective dep ON dep.UserId=q.UserId AND dep.PermissionKey=N'View'
WHERE q.EntityKey=N'QualityControl' AND q.PermissionKey=N'View' AND q.EffectiveScope=2
  AND dep.EntityKey IN(N'Customer',N'Deal',N'Factor',N'QualityControlQuestion')
  AND dep.EffectiveScope=0
ORDER BY u.UserName,dep.EntityKey;
