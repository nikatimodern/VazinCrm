using System;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    [DbContext(typeof(CRMContextDb))]
    [Migration("20261117231800_AddQualityControlContactFollowUp")]
    public partial class AddQualityControlContactFollowUp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvalidMobileAtMark",
                table: "QualityControlCustomerContacts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastNoResponseAt",
                table: "QualityControlCustomerContacts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NoResponseAttempts",
                table: "QualityControlCustomerContacts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Preserve older attempts and freeze the current number as the baseline for
            // existing invalid-phone contacts. No QC item result is modified.
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.NoResponseAttempts = 1,
                    c.LastNoResponseAt = COALESCE(c.ModifyDate, c.CreateDate)
                FROM dbo.QualityControlCustomerContacts AS c
                WHERE c.Status = 1;

                UPDATE c
                SET c.InvalidMobileAtMark = LEFT(LTRIM(RTRIM(ISNULL(u.Mobile, N''))), 50)
                FROM dbo.QualityControlCustomerContacts AS c
                INNER JOIN dbo.Customers AS u ON u.Id = c.CustomerId
                WHERE c.Status = 2;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "InvalidMobileAtMark", table: "QualityControlCustomerContacts");
            migrationBuilder.DropColumn(name: "LastNoResponseAt", table: "QualityControlCustomerContacts");
            migrationBuilder.DropColumn(name: "NoResponseAttempts", table: "QualityControlCustomerContacts");
        }
    }
}
