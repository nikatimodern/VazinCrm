using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140504231720 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CashGroupId",
                table: "Cashes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CashGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    table.PrimaryKey("PK_CashGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cashes_CashGroupId",
                table: "Cashes",
                column: "CashGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CashGroups_GroupNumber",
                table: "CashGroups",
                column: "GroupNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cashes_CashGroups_CashGroupId",
                table: "Cashes",
                column: "CashGroupId",
                principalTable: "CashGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cashes_CashGroups_CashGroupId",
                table: "Cashes");

            migrationBuilder.DropTable(
                name: "CashGroups");

            migrationBuilder.DropIndex(
                name: "IX_Cashes_CashGroupId",
                table: "Cashes");

            migrationBuilder.DropColumn(
                name: "CashGroupId",
                table: "Cashes");
        }
    }
}
