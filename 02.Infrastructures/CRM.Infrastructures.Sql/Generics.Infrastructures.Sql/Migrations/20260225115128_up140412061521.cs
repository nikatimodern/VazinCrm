using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140412061521 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashierUsers");

            migrationBuilder.RenameColumn(
                name: "CostTitle",
                table: "Cashes",
                newName: "CostTitleTxt");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Cashiers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserTxt",
                table: "Cashiers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "Cashes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CostCenterTxt",
                table: "Cashes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CostTitleId",
                table: "Cashes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Cashiers");

            migrationBuilder.DropColumn(
                name: "UserTxt",
                table: "Cashiers");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "Cashes");

            migrationBuilder.DropColumn(
                name: "CostCenterTxt",
                table: "Cashes");

            migrationBuilder.DropColumn(
                name: "CostTitleId",
                table: "Cashes");

            migrationBuilder.RenameColumn(
                name: "CostTitleTxt",
                table: "Cashes",
                newName: "CostTitle");

            migrationBuilder.CreateTable(
                name: "CashierUsers",
                columns: table => new
                {
                    CashierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashierUsers", x => new { x.CashierId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CashierUsers_Cashiers_CashierId",
                        column: x => x.CashierId,
                        principalTable: "Cashiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
