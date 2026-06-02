using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140503121144 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Prepayment",
                table: "RequestAmounts");

            migrationBuilder.DropColumn(
                name: "Vosoled",
                table: "RequestAmounts");

            migrationBuilder.AddColumn<Guid>(
                name: "CallRecordingId",
                table: "Reports",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_CallRecordingId",
                table: "Reports",
                column: "CallRecordingId",
                unique: true,
                filter: "[CallRecordingId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_CallRecordings_CallRecordingId",
                table: "Reports",
                column: "CallRecordingId",
                principalTable: "CallRecordings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_CallRecordings_CallRecordingId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_CallRecordingId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "CallRecordingId",
                table: "Reports");

            migrationBuilder.AddColumn<bool>(
                name: "Prepayment",
                table: "RequestAmounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Vosoled",
                table: "RequestAmounts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
