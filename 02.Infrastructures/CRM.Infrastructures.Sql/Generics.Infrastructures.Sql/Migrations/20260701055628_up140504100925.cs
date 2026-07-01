using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140504100925 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiError",
                table: "CallRecordings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiStatus",
                table: "CallRecordings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AiStatusTxt",
                table: "CallRecordings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AvanegarAttempts",
                table: "CallRecordings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AvanegarRequestId",
                table: "CallRecordings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "CallRecordings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTrackDate",
                table: "CallRecordings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextTrackDate",
                table: "CallRecordings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiError",
                table: "CallRecordings");

            migrationBuilder.DropColumn(
                name: "AiStatus",
                table: "CallRecordings");

            migrationBuilder.DropColumn(
                name: "AiStatusTxt",
                table: "CallRecordings");

            migrationBuilder.DropColumn(
                name: "AvanegarAttempts",
                table: "CallRecordings");

            migrationBuilder.DropColumn(
                name: "AvanegarRequestId",
                table: "CallRecordings");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "CallRecordings");

            migrationBuilder.DropColumn(
                name: "LastTrackDate",
                table: "CallRecordings");

            migrationBuilder.DropColumn(
                name: "NextTrackDate",
                table: "CallRecordings");
        }
    }
}
