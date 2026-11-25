using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations
{
    public partial class AddPrefactorTermsCreateDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Accept databases where the previously supplied SQL script was applied.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.BaseDescriptions', N'CreateDate') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[BaseDescriptions] ADD [CreateDate] datetime2(7) NULL;
                END;
                ELSE IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'dbo.BaseDescriptions')
                      AND name = N'CreateDate'
                      AND system_type_id = TYPE_ID(N'datetime2')
                      AND scale = 7 AND is_nullable = 1
                )
                BEGIN
                    THROW 50001, 'Existing BaseDescriptions.CreateDate has an incompatible definition.', 1;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreateDate",
                table: "BaseDescriptions");
        }
    }
}
