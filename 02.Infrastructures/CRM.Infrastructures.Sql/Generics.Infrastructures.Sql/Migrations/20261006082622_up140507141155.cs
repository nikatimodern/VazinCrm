using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507141155 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContactName",
                table: "SentSmses",
                newName: "ReceivedFullName");

            migrationBuilder.RenameColumn(
                name: "ContactName",
                table: "ReceivedSmses",
                newName: "SenderFullName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReceivedFullName",
                table: "SentSmses",
                newName: "ContactName");

            migrationBuilder.RenameColumn(
                name: "SenderFullName",
                table: "ReceivedSmses",
                newName: "ContactName");
        }
    }
}
