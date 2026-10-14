using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507151320 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentText",
                table: "BankAccountExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "CreateDate",
                table: "BankAccountExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "CreateDateTxt",
                table: "BankAccountExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "BankAccountExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "ModifyDate",
                table: "BankAccountExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "ModifyDateTxt",
                table: "BankAccountExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "UserCreator",
                table: "BankAccountExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "UserCreatorID",
                table: "BankAccountExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "UserModify",
                table: "BankAccountExternalSoftwares");

            migrationBuilder.DropColumn(
                name: "UserModifyID",
                table: "BankAccountExternalSoftwares");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommentText",
                table: "BankAccountExternalSoftwares",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDate",
                table: "BankAccountExternalSoftwares",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreateDateTxt",
                table: "BankAccountExternalSoftwares",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "BankAccountExternalSoftwares",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifyDate",
                table: "BankAccountExternalSoftwares",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifyDateTxt",
                table: "BankAccountExternalSoftwares",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserCreator",
                table: "BankAccountExternalSoftwares",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserCreatorID",
                table: "BankAccountExternalSoftwares",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserModify",
                table: "BankAccountExternalSoftwares",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserModifyID",
                table: "BankAccountExternalSoftwares",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
