using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507071645 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceivedSmses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FarazId = table.Column<long>(type: "bigint", nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceivedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtFaraz = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Seen = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ReceivedSmses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedSmses_FarazId",
                table: "ReceivedSmses",
                column: "FarazId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceivedSmses");
        }
    }
}
