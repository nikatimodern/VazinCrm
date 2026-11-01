using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140508101618 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountancyTransferId",
                table: "Accountancies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountancyTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountancyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Beneficiary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToName = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_AccountancyTransfers", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accountancies_AccountancyTransfers_AccountancyTransferId",
                table: "Accountancies");

            migrationBuilder.DropTable(
                name: "AccountancyTransfers");

            migrationBuilder.DropIndex(
                name: "IX_Accountancies_AccountancyTransferId",
                table: "Accountancies");

            migrationBuilder.DropColumn(
                name: "AccountancyTransferId",
                table: "Accountancies");
        }
    }
}
