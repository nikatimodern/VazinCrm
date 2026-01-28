using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140414108 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FinalResultCallId",
                table: "Reports",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalResultCallTxt",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReasonCancelId",
                table: "Reports",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonCancelTxt",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResultCallId",
                table: "Reports",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultCallTxt",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalResultCallId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "FinalResultCallTxt",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ReasonCancelId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ReasonCancelTxt",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ResultCallId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ResultCallTxt",
                table: "Reports");
        }
    }
}
