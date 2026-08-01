using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140504271223 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportParticipants_CallRecordings_CallRecordingId",
                table: "ReportParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportParticipants_Reports_ReportId",
                table: "ReportParticipants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReportParticipants",
                table: "ReportParticipants");

            migrationBuilder.RenameTable(
                name: "ReportParticipants",
                newName: "CallParticipants");

            migrationBuilder.RenameIndex(
                name: "IX_ReportParticipants_ReportId",
                table: "CallParticipants",
                newName: "IX_CallParticipants_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_ReportParticipants_CallRecordingId",
                table: "CallParticipants",
                newName: "IX_CallParticipants_CallRecordingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CallParticipants",
                table: "CallParticipants",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CallParticipants_CallRecordings_CallRecordingId",
                table: "CallParticipants",
                column: "CallRecordingId",
                principalTable: "CallRecordings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CallParticipants_Reports_ReportId",
                table: "CallParticipants",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallParticipants_CallRecordings_CallRecordingId",
                table: "CallParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_CallParticipants_Reports_ReportId",
                table: "CallParticipants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CallParticipants",
                table: "CallParticipants");

            migrationBuilder.RenameTable(
                name: "CallParticipants",
                newName: "ReportParticipants");

            migrationBuilder.RenameIndex(
                name: "IX_CallParticipants_ReportId",
                table: "ReportParticipants",
                newName: "IX_ReportParticipants_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_CallParticipants_CallRecordingId",
                table: "ReportParticipants",
                newName: "IX_ReportParticipants_CallRecordingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReportParticipants",
                table: "ReportParticipants",
                column: "Id");

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
    }
}
