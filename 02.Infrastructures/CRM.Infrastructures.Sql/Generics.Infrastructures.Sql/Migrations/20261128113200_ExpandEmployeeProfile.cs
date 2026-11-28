using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations
{
    // Additive migration; existing personnel rows remain intact and new fields are optional.
    public partial class ExpandEmployeeProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Address", table: "HrEmployeeProfiles", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "BirthCertificateNumber", table: "HrEmployeeProfiles", type: "nvarchar(32)", maxLength: 32, nullable: true);
            migrationBuilder.AddColumn<DateOnly>(name: "BirthDate", table: "HrEmployeeProfiles", type: "date", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Education", table: "HrEmployeeProfiles", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Email", table: "HrEmployeeProfiles", type: "nvarchar(254)", maxLength: 254, nullable: true);
            migrationBuilder.AddColumn<string>(name: "EmergencyContactName", table: "HrEmployeeProfiles", type: "nvarchar(150)", maxLength: 150, nullable: true);
            migrationBuilder.AddColumn<string>(name: "EmergencyContactPhone", table: "HrEmployeeProfiles", type: "nvarchar(24)", maxLength: 24, nullable: true);
            migrationBuilder.AddColumn<string>(name: "FatherName", table: "HrEmployeeProfiles", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Mobile", table: "HrEmployeeProfiles", type: "nvarchar(24)", maxLength: 24, nullable: true);
            migrationBuilder.AddColumn<string>(name: "NationalCode", table: "HrEmployeeProfiles", type: "nvarchar(10)", maxLength: 10, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Telephone", table: "HrEmployeeProfiles", type: "nvarchar(24)", maxLength: 24, nullable: true);
            migrationBuilder.CreateIndex(name: "IX_HrEmployeeProfiles_NationalCode", table: "HrEmployeeProfiles",
                column: "NationalCode", unique: true, filter: "[NationalCode] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverting removes the new profile details. Back up before rollback.
            migrationBuilder.DropIndex(name: "IX_HrEmployeeProfiles_NationalCode", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "Telephone", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "NationalCode", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "Mobile", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "FatherName", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "EmergencyContactPhone", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "EmergencyContactName", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "Email", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "Education", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "BirthDate", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "BirthCertificateNumber", table: "HrEmployeeProfiles");
            migrationBuilder.DropColumn(name: "Address", table: "HrEmployeeProfiles");
        }
    }
}
