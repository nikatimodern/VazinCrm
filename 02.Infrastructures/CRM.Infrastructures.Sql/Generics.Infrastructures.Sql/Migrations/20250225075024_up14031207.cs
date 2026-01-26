using Microsoft.EntityFrameworkCore.Migrations;

namespace CRM.Infrastructures.Sql.Migrations
{
    public partial class up14031207 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "completed",
                table: "TaskUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AssignUser",
                table: "Carizos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignUserId",
                table: "Carizos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompleteTaskUser",
                table: "Carizos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompleteTaskUserId",
                table: "Carizos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShareTaskId",
                table: "Carizos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaskType",
                table: "Carizos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TaskTypeTxt",
                table: "Carizos",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "completed",
                table: "TaskUsers");

            migrationBuilder.DropColumn(
                name: "AssignUser",
                table: "Carizos");

            migrationBuilder.DropColumn(
                name: "AssignUserId",
                table: "Carizos");

            migrationBuilder.DropColumn(
                name: "CompleteTaskUser",
                table: "Carizos");

            migrationBuilder.DropColumn(
                name: "CompleteTaskUserId",
                table: "Carizos");

            migrationBuilder.DropColumn(
                name: "ShareTaskId",
                table: "Carizos");

            migrationBuilder.DropColumn(
                name: "TaskType",
                table: "Carizos");

            migrationBuilder.DropColumn(
                name: "TaskTypeTxt",
                table: "Carizos");
        }
    }
}
