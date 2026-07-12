using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140504211212 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportParticipants_Reports_ReportId",
                table: "ReportParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_CallRecordings_CallRecordingId",
                table: "Reports");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReportId",
                table: "ReportParticipants",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "CallRecordingId",
                table: "ReportParticipants",
                type: "uniqueidentifier",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_ReportParticipants_CallRecordingId",
                table: "ReportParticipants",
                column: "CallRecordingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportParticipants_CallRecordings_CallRecordingId",
                table: "ReportParticipants",
                column: "CallRecordingId",
                principalTable: "CallRecordings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportParticipants_Reports_ReportId",
                table: "ReportParticipants",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_CallRecordings_CallRecordingId",
                table: "Reports",
                column: "CallRecordingId",
                principalTable: "CallRecordings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportParticipants_CallRecordings_CallRecordingId",
                table: "ReportParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportParticipants_Reports_ReportId",
                table: "ReportParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_CallRecordings_CallRecordingId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_ReportParticipants_CallRecordingId",
                table: "ReportParticipants");

            migrationBuilder.DropColumn(
                name: "CallRecordingId",
                table: "ReportParticipants");

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

            migrationBuilder.AlterColumn<Guid>(
                name: "ReportId",
                table: "ReportParticipants",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportParticipants_Reports_ReportId",
                table: "ReportParticipants",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_CallRecordings_CallRecordingId",
                table: "Reports",
                column: "CallRecordingId",
                principalTable: "CallRecordings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
