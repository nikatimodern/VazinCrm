using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140508101846 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accountancies_AccountancyTransfers_AccountancyTransferId",
                table: "Accountancies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AccountancyTransfers",
                table: "AccountancyTransfers");

            migrationBuilder.DropIndex(
                name: "IX_Accountancies_AccountancyTransferId",
                table: "Accountancies");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AccountancyTransfers");

            migrationBuilder.DropColumn(
                name: "CommentText",
                table: "AccountancyTransfers");

            migrationBuilder.DropColumn(
                name: "UserCreator",
                table: "AccountancyTransfers");

            migrationBuilder.DropColumn(
                name: "UserCreatorID",
                table: "AccountancyTransfers");

            migrationBuilder.DropColumn(
                name: "UserModify",
                table: "AccountancyTransfers");

            migrationBuilder.DropColumn(
                name: "UserModifyID",
                table: "AccountancyTransfers");

            migrationBuilder.DropColumn(
                name: "AccountancyTransferId",
                table: "Accountancies");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccountancyTransfers",
                table: "AccountancyTransfers",
                column: "AccountancyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountancyTransfers_Accountancies_AccountancyId",
                table: "AccountancyTransfers",
                column: "AccountancyId",
                principalTable: "Accountancies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountancyTransfers_Accountancies_AccountancyId",
                table: "AccountancyTransfers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AccountancyTransfers",
                table: "AccountancyTransfers");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "AccountancyTransfers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CommentText",
                table: "AccountancyTransfers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserCreator",
                table: "AccountancyTransfers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserCreatorID",
                table: "AccountancyTransfers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserModify",
                table: "AccountancyTransfers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserModifyID",
                table: "AccountancyTransfers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AccountancyTransferId",
                table: "Accountancies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccountancyTransfers",
                table: "AccountancyTransfers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Accountancies_AccountancyTransferId",
                table: "Accountancies",
                column: "AccountancyTransferId",
                unique: true,
                filter: "[AccountancyTransferId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Accountancies_AccountancyTransfers_AccountancyTransferId",
                table: "Accountancies",
                column: "AccountancyTransferId",
                principalTable: "AccountancyTransfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
