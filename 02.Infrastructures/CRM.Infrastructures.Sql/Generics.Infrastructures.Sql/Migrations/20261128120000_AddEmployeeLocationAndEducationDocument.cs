using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations
{
    public partial class AddEmployeeLocationAndEducationDocument : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(name: "Latitude", table: "HrEmployeeProfiles", type: "decimal(9,6)", precision: 9, scale: 6, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "Longitude", table: "HrEmployeeProfiles", type: "decimal(9,6)", precision: 9, scale: 6, nullable: true);
            migrationBuilder.AddColumn<byte[]>(name: "EducationDocumentContent", table: "HrEmployeeProfiles", type: "varbinary(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "EducationDocumentType", table: "HrEmployeeProfiles", type: "nvarchar(32)", maxLength: 32, nullable: true);
            migrationBuilder.AddCheckConstraint(name: "CK_HrEmployeeProfiles_Coordinates", table: "HrEmployeeProfiles", sql: "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL AND [Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "CK_HrEmployeeProfiles_Coordinates", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "EducationDocumentType", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "EducationDocumentContent", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "Longitude", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "Latitude", table: "HrEmployeeProfiles");
        }
    }
}
