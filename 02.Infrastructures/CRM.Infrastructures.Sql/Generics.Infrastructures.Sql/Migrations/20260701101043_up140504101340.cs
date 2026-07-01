using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140504101340 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportParticipants_Reports_ReportId",
                table: "ReportParticipants");

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

            migrationBuilder.DropIndex(
                name: "IX_ReportParticipants_CallRecordingId",
                table: "ReportParticipants");

            migrationBuilder.DropColumn(
                name: "CallRecordingId",
                table: "ReportParticipants");

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
        }
    }
}
