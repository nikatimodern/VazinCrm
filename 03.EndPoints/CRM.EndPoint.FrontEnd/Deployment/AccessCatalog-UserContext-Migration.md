# AccessCatalog: migration from CRMContextDb to UserContextDb (DEV/TEST)

On branch `AccLevel`, the two experimental migrations for `CRMContextDb` have been **removed from source**. The access catalog now belongs to `UserContextDb`. Data in these experimental access tables is intentionally not preserved.

## Required steps in the test database

1. Confirm the actual SQL Server database behind `CRMConnection` and `UserConnection`. Back up the test databases before changing their schema.
2. Using SSMS against the database configured by **CRMConnection**, run `DEV_RollbackAccessCatalogFromCrm.sql`. It drops only `AccessRolePermissions`, `AccessPermissions`, `AccessEntities`, and deletes only the history entries for `20261110180000_AddAccessCatalog` and `20261110200000_AddAccessRolePermissions`. It works whether these two test migrations have been applied or not.
3. Compile the new code and apply the one new migration in the **UserConnection** database, which creates the three catalog tables with a real foreign key to `AspNetRoles`:
   
   From Visual Studio Package Manager Console, with Default Project `CRM.Infrastructures.Sql`:
   
   `Update-Database -Context UserContextDb`
   
   Or from the repository root:
   
   `dotnet ef database update --context UserContextDb --project "02.Infrastructures/CRM.Infrastructures.Sql/Generics.Infrastructures.Sql/CRM.Infrastructures.Sql.csproj" --startup-project "03.EndPoints/CRM.EndPoint.FrontEnd/CRM.EndPoint.FrontEnd.csproj"`

4. Verify all three tables are in the UserConnection database, and verify `FK_AccessRolePermissions_AspNetRoles_RoleId`. Test entity creation and role permission saves.
5. Do NOT reset the Identity or CRM database, and do not delete any other migrations. No new `CRMContextDb` migration is needed for this experimental rollback because the two trial migration files were removed from source.

If both connections target the same database, **step 2 must run before step 3** or the table creation will fail.


## Dynamic user access (second UserContextDb migration)

The user-permission feature adds `20261111100000_AddAccessUserPermissions` in `UserContextDbMigrations`.
Apply pending migrations with:

`Update-Database -Context UserContextDb`

This creates only `AccessUserPermissions` (composite key: `UserId`, `AccessPermissionId`),
with foreign keys to `AspNetUsers` and `AccessPermissions`. It does **not** reset users,
roles, their existing memberships, position values, or catalog definitions.

A user's effective access comes from the **current union of permissions assigned to all
their Identity roles**; the role permissions are never copied into the user table.
`AccessUserPermissions` stores only overrides:

- no row: inherit from the user's roles;
- `IsAllowed = 1`: explicit allow;
- `IsAllowed = 0`: explicit deny (wins even if multiple roles grant it).

Inactive access entities and permissions are not grantable. The User Access tab supports
multiple role memberships and all three override states. Saving roles plus overrides is
transactional. No application controller or API permission enforcement has been enabled
yet; the page is for administration and preview.
