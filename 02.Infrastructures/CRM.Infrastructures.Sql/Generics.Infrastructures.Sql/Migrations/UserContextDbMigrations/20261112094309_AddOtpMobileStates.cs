using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations
{
    /// <inheritdoc />
    public partial class AddOtpMobileStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OtpMobileStates",
                columns: table => new
                {
                    Mobile = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    OtpType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CodeSalt = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CodeHash = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    OtpExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutCount = table.Column<int>(type: "int", nullable: false),
                    LockedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpMobileStates", x => x.Mobile);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtpMobileStates");
        }
    }
}
