using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140504251158 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvanegarAttempts",
                table: "CallRecordings",
                newName: "AiAttempts");

            migrationBuilder.AddColumn<double>(
                name: "AiDurationSeconds",
                table: "CallRecordings",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiDurationSeconds",
                table: "CallRecordings");

            migrationBuilder.RenameColumn(
                name: "AiAttempts",
                table: "CallRecordings",
                newName: "AvanegarAttempts");
        }
    }
}
