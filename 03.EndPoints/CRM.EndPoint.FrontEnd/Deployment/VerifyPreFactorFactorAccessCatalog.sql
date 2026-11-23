/*
  PreFactor & Factor AccessCatalog verification
  Database: UserConnection / UserContextDb
  READ-ONLY. No UPDATE / DELETE / INSERT.
  Run after migrations:
    20261123234000_RegisterPreFactorFactorAccessEntities
    20261123234100_RegisterPreFactorFactorPermissions
    20261123234200_SeedPreFactorFactorRoleGrants
*/
SET NOCOUNT ON;

-- 1) Entities, model keys and available permissions.
SELECT e.[Key] AS EntityKey, e.Title AS EntityTitle, e.DbContextName,
       e.ModelFullName, e.IsActive, p.[Key] AS PermissionKey,
       p.Title AS PermissionTitle, p.SupportsScope, p.IsActive AS PermissionActive
FROM dbo.AccessEntities e
LEFT JOIN dbo.AccessPermissions p ON p.AccessEntityId = e.Id
WHERE e.[Key] IN (N'PreFactor', N'Factor')
ORDER BY e.[Key], p.[Key];

-- 2) Role permissions, including 1=Own and 2=All.
SELECT r.[Name] AS RoleName, e.[Key] AS EntityKey,
       p.[Key] AS PermissionKey, rp.Scope,
       CASE rp.Scope WHEN 1 THEN N'خود/مرتبط' WHEN 2 THEN N'همه' ELSE N'نامعتبر' END AS ScopeName,
       COUNT(ur.UserId) AS MembersInRole
FROM dbo.AccessRolePermissions rp
JOIN dbo.AspNetRoles r ON r.Id = rp.RoleId
JOIN dbo.AccessPermissions p ON p.Id = rp.AccessPermissionId
JOIN dbo.AccessEntities e ON e.Id = p.AccessEntityId
LEFT JOIN dbo.AspNetUserRoles ur ON ur.RoleId = r.Id
WHERE e.[Key] IN (N'PreFactor', N'Factor')
GROUP BY r.[Name], e.[Key], p.[Key], rp.Scope
ORDER BY e.[Key], r.[Name], p.[Key];

-- 3) Employee-effective permission preview. Role union followed by explicit
--    user override, consistent with AccessScopeService.GetEffectiveScopeAsync.
--    No override row means inheritance; no matching role grant means Deny.
WITH Effective AS (
    SELECT u.Id AS UserId, u.UserName, u.FirstName, u.LastName,
           e.[Key] AS EntityKey, p.[Key] AS PermissionKey,
           up.IsAllowed AS ExplicitIsAllowed, up.Scope AS ExplicitScope,
           MAX(rp.Scope) AS InheritedScope
    FROM dbo.AspNetUsers u
    CROSS JOIN dbo.AccessPermissions p
    JOIN dbo.AccessEntities e ON e.Id = p.AccessEntityId
    LEFT JOIN dbo.AspNetUserRoles ur ON ur.UserId = u.Id
    LEFT JOIN dbo.AccessRolePermissions rp ON rp.RoleId = ur.RoleId
         AND rp.AccessPermissionId = p.Id
    LEFT JOIN dbo.AccessUserPermissions up ON up.UserId = u.Id
         AND up.AccessPermissionId = p.Id
    WHERE e.[Key] IN (N'PreFactor', N'Factor')
      AND e.IsActive = 1 AND p.IsActive = 1
      AND u.IsEmployee = 1
    GROUP BY u.Id, u.UserName, u.FirstName, u.LastName,
             e.[Key], p.[Key], up.IsAllowed, up.Scope
)
SELECT UserId, UserName, FirstName, LastName, EntityKey, PermissionKey,
       CASE WHEN ExplicitIsAllowed IS NOT NULL THEN N'اختصاصی' ELSE N'از نقش‌ها' END AS Origin,
       CASE WHEN ExplicitIsAllowed = 0 THEN 0
            WHEN ExplicitIsAllowed = 1 THEN ExplicitScope
            ELSE COALESCE(InheritedScope, 0) END AS EffectiveScope
FROM Effective
ORDER BY UserName, EntityKey, PermissionKey;

-- 4) Counts; expected catalog: 2 entities, 21 permissions.
SELECT e.[Key] AS EntityKey,
       COUNT(DISTINCT p.Id) AS PermissionCount,
       COUNT(rp.RoleId) AS RoleGrantCount,
       COUNT(DISTINCT up.UserId) AS DirectUserOverrideCount
FROM dbo.AccessEntities e
LEFT JOIN dbo.AccessPermissions p ON p.AccessEntityId = e.Id
LEFT JOIN dbo.AccessRolePermissions rp ON rp.AccessPermissionId = p.Id
LEFT JOIN dbo.AccessUserPermissions up ON up.AccessPermissionId = p.Id
WHERE e.[Key] IN (N'PreFactor', N'Factor')
GROUP BY e.[Key];
