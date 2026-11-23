using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations;

[DbContext(typeof(UserContextDb))]
[Migration("20261123234000_RegisterPreFactorFactorAccessEntities")]
public partial class RegisterPreFactorFactorAccessEntities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Idempotent data migration: do not overwrite customized catalog entries.
        migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM dbo.AccessEntities WHERE [Key] = N'PreFactor'
           AND (DbContextName <> N'CRMContextDb' OR ModelFullName <> N'CRM.Core.Domain.PreFactors.Entities.PreFactor'))
    THROW 51021, 'AccessCatalog PreFactor key belongs to a different model.', 1;
IF EXISTS (SELECT 1 FROM dbo.AccessEntities WHERE [Key] = N'Factor'
           AND (DbContextName <> N'CRMContextDb' OR ModelFullName <> N'CRM.Core.Domain.Factors.Entities.Factor'))
    THROW 51022, 'AccessCatalog Factor key belongs to a different model.', 1;
IF EXISTS (SELECT 1 FROM dbo.AccessEntities WHERE DbContextName = N'CRMContextDb'
           AND ModelFullName = N'CRM.Core.Domain.PreFactors.Entities.PreFactor' AND [Key] <> N'PreFactor')
    THROW 51023, 'PreFactor model is already registered using another key.', 1;
IF EXISTS (SELECT 1 FROM dbo.AccessEntities WHERE DbContextName = N'CRMContextDb'
           AND ModelFullName = N'CRM.Core.Domain.Factors.Entities.Factor' AND [Key] <> N'Factor')
    THROW 51024, 'Factor model is already registered using another key.', 1;
INSERT INTO dbo.AccessEntities (Id,[Key],Title,[Description],DbContextName,ModelFullName,IsActive,CreatedAtUtc)
SELECT NEWID(),v.EntityKey,v.Title,v.[Description],N'CRMContextDb',v.ModelFullName,1,SYSUTCDATETIME()
FROM (VALUES
  (N'PreFactor',N'پیش‌فاکتورها',N'مدیریت پیش‌فاکتور و گردش تأییدها',N'CRM.Core.Domain.PreFactors.Entities.PreFactor'),
  (N'Factor',N'فاکتورها',N'مدیریت فاکتور فروش و ارتباط مالی',N'CRM.Core.Domain.Factors.Entities.Factor')
) AS v(EntityKey,Title,[Description],ModelFullName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.AccessEntities e WHERE e.[Key] = v.EntityKey);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally keep permission data and administrator changes on rollback.
        // Remove catalog entries only through the audited AccessCatalog UI.
    }
}
