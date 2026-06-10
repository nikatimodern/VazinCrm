using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140503191646 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "receiver",
                table: "Checks",
                newName: "Payee");

            migrationBuilder.RenameColumn(
                name: "IssueDate",
                table: "Checks",
                newName: "DueDateMiladi");

            migrationBuilder.AlterColumn<string>(
                name: "DueDate",
                table: "Checks",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Payee",
                table: "Checks",
                newName: "receiver");

            migrationBuilder.RenameColumn(
                name: "DueDateMiladi",
                table: "Checks",
                newName: "IssueDate");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "Checks",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
