using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CRM.Infrastructures.Sql.Migrations
{
    public partial class up14031129 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemindDays");

            migrationBuilder.RenameColumn(
                name: "TaskTitle",
                table: "Carizos",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "TaskRemindStatus",
                table: "Carizos",
                newName: "TaskStatus");

            migrationBuilder.RenameColumn(
                name: "TaskDesc",
                table: "Carizos",
                newName: "RepeatUntil");

            migrationBuilder.RenameColumn(
                name: "RemindType",
                table: "Carizos",
                newName: "RepeatType");

            migrationBuilder.RenameColumn(
                name: "RemindDateM",
                table: "Carizos",
                newName: "DateMiladi");

            migrationBuilder.RenameColumn(
                name: "RemindDate",
                table: "Carizos",
                newName: "RepeatDays");

            migrationBuilder.AddColumn<string>(
                name: "Date",
                table: "Carizos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Carizos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Remind",
                table: "Carizos",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RemindStatus",
                table: "Carizos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RemindTime",
                table: "Carizos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RepeatUntilMiladi",
                table: "Carizos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "TaskUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UserCreatorID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserCreator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModifyID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModify = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskUsers_Carizos_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Carizos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskUsers_TaskId",
                table: "TaskUsers",
                column: "TaskId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskUsers");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Carizos");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Carizos");

            migrationBuilder.DropColumn(
                name: "Remind",
                table: "Carizos");

            migrationBuilder.DropColumn(
                name: "RemindStatus",
                table: "Carizos");

            migrationBuilder.DropColumn(
                name: "RemindTime",
                table: "Carizos");

            migrationBuilder.DropColumn(
                name: "RepeatUntilMiladi",
                table: "Carizos");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Carizos",
                newName: "TaskTitle");

            migrationBuilder.RenameColumn(
                name: "TaskStatus",
                table: "Carizos",
                newName: "TaskRemindStatus");

            migrationBuilder.RenameColumn(
                name: "RepeatUntil",
                table: "Carizos",
                newName: "TaskDesc");

            migrationBuilder.RenameColumn(
                name: "RepeatType",
                table: "Carizos",
                newName: "RemindType");

            migrationBuilder.RenameColumn(
                name: "RepeatDays",
                table: "Carizos",
                newName: "RemindDate");

            migrationBuilder.RenameColumn(
                name: "DateMiladi",
                table: "Carizos",
                newName: "RemindDateM");

            migrationBuilder.CreateTable(
                name: "RemindDays",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarizoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Day = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    value = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemindDays", x => x.id);
                    table.ForeignKey(
                        name: "FK_RemindDays_Carizos_CarizoId",
                        column: x => x.CarizoId,
                        principalTable: "Carizos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RemindDays_CarizoId",
                table: "RemindDays",
                column: "CarizoId");
        }
    }
}
