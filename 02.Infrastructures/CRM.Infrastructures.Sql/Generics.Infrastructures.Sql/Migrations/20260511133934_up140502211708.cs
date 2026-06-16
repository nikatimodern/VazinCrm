using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140502211708 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CallerCustomerId",
                table: "CallRecordings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CallerCustomerTxt",
                table: "CallRecordings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationUserId",
                table: "CallRecordings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationUserTxt",
                table: "CallRecordings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CallerCustomerId",
                table: "CallRecordings");

            migrationBuilder.DropColumn(
                name: "CallerCustomerTxt",
                table: "CallRecordings");

            migrationBuilder.DropColumn(
                name: "DestinationUserId",
                table: "CallRecordings");

            migrationBuilder.DropColumn(
                name: "DestinationUserTxt",
                table: "CallRecordings");
        }
    }
}
