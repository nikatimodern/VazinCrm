using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up14040905 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FinancialStep",
                table: "PreFactors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChangedById",
                table: "logSystems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cartables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromCartable = table.Column<int>(type: "int", nullable: true),
                    FromCartableTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToCartable = table.Column<int>(type: "int", nullable: true),
                    ToCartableTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogSystemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Cartables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cartables_logSystems_LogSystemId",
                        column: x => x.LogSystemId,
                        principalTable: "logSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cartables_LogSystemId",
                table: "Cartables",
                column: "LogSystemId",
                unique: true,
                filter: "[LogSystemId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cartables");

            migrationBuilder.DropColumn(
                name: "FinancialStep",
                table: "PreFactors");

            migrationBuilder.DropColumn(
                name: "ChangedById",
                table: "logSystems");
        }
    }
}
