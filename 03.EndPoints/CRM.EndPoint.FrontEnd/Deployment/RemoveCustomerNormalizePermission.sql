/*
Run against the UserContextDb / UserConnection database, not CRMContextDb.
Removes ONLY the retired Customer.Normalize catalog permission and its grants.
Run after taking a database backup / AccessCatalog restore point.
Existing historical restore points may reintroduce this retired permission.
The application migration 20261123153000 performs this same cleanup.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.AccessEntities', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AccessPermissions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AccessRolePermissions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AccessUserPermissions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AccessUserScopeMembers', N'U') IS NULL
    THROW 53111, N'AccessCatalog tables not found in the selected database.', 1;

PRINT N'Selected database: ' + DB_NAME();
BEGIN TRANSACTION;

DECLARE @ObsoletePermissions TABLE (Id uniqueidentifier NOT NULL PRIMARY KEY);
INSERT INTO @ObsoletePermissions(Id)
SELECT p.Id
FROM dbo.AccessPermissions AS p
INNER JOIN dbo.AccessEntities AS e ON e.Id = p.AccessEntityId
WHERE e.[Key] = N'Customer'
  AND p.[Key] IN (N'Normalize', N'Customer.Normalize');

DECLARE @PermissionCount int = (SELECT COUNT(*) FROM @ObsoletePermissions);
PRINT N'Obsolete Customer.Normalize permissions: ' + CAST(@PermissionCount AS nvarchar(10));

DELETE scopeMember
FROM dbo.AccessUserScopeMembers AS scopeMember
INNER JOIN @ObsoletePermissions AS obsolete ON obsolete.Id = scopeMember.AccessPermissionId;

DELETE rolePermission
FROM dbo.AccessRolePermissions AS rolePermission
INNER JOIN @ObsoletePermissions AS obsolete ON obsolete.Id = rolePermission.AccessPermissionId;

DELETE userPermission
FROM dbo.AccessUserPermissions AS userPermission
INNER JOIN @ObsoletePermissions AS obsolete ON obsolete.Id = userPermission.AccessPermissionId;

DELETE permission
FROM dbo.AccessPermissions AS permission
INNER JOIN @ObsoletePermissions AS obsolete ON obsolete.Id = permission.Id;

COMMIT TRANSACTION;

PRINT N'Retired permission removed. Other Customer and Deal permissions were preserved.';
