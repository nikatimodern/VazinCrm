using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up14040624 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DealCode",
                table: "Factors");

            migrationBuilder.AddColumn<int>(
                name: "DealSerialNumber",
                table: "Factors",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DealSerialNumber",
                table: "Factors");

            migrationBuilder.AddColumn<string>(
                name: "DealCode",
                table: "Factors",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
