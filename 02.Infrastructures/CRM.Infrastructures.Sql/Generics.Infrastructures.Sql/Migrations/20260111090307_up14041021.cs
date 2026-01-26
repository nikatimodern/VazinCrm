using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations
{
    /// <inheritdoc />
    public partial class up14041021 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromUserId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "FromUserTxt",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "ToUserId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "ToUserTxt",
                table: "Chats");

            migrationBuilder.AddColumn<Guid>(
                name: "MainChatId",
                table: "Chats",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "MainChats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromUserTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToUserTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_MainChats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_MainChatId",
                table: "Chats",
                column: "MainChatId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_MainChats_MainChatId",
                table: "Chats",
                column: "MainChatId",
                principalTable: "MainChats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_MainChats_MainChatId",
                table: "Chats");

            migrationBuilder.DropTable(
                name: "MainChats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_MainChatId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "MainChatId",
                table: "Chats");

            migrationBuilder.AddColumn<string>(
                name: "FromUserId",
                table: "Chats",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromUserTxt",
                table: "Chats",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToUserId",
                table: "Chats",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToUserTxt",
                table: "Chats",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
