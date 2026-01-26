using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations
{
    public partial class init1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentText",
                table: "BaseAdmins");

            migrationBuilder.DropColumn(
                name: "CreateDate",
                table: "BaseAdmins");

            migrationBuilder.DropColumn(
                name: "CreateDateTxt",
                table: "BaseAdmins");

            migrationBuilder.DropColumn(
                name: "ModifyDate",
                table: "BaseAdmins");

            migrationBuilder.DropColumn(
                name: "ModifyDateTxt",
                table: "BaseAdmins");

            migrationBuilder.DropColumn(
                name: "UserCreator",
                table: "BaseAdmins");

            migrationBuilder.DropColumn(
                name: "UserCreatorID",
                table: "BaseAdmins");

            migrationBuilder.DropColumn(
                name: "UserModify",
                table: "BaseAdmins");

            migrationBuilder.DropColumn(
                name: "UserModifyID",
                table: "BaseAdmins");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommentText",
                table: "BaseAdmins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDate",
                table: "BaseAdmins",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreateDateTxt",
                table: "BaseAdmins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifyDate",
                table: "BaseAdmins",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifyDateTxt",
                table: "BaseAdmins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserCreator",
                table: "BaseAdmins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserCreatorID",
                table: "BaseAdmins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserModify",
                table: "BaseAdmins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserModifyID",
                table: "BaseAdmins",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
