using Microsoft.EntityFrameworkCore.Migrations;

namespace CRM.Infrastructures.Sql.Migrations
{
    public partial class _14031130 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Repeat",
                table: "Carizos",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepeatTypeTxt",
                table: "Carizos",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Repeat",
                table: "Carizos");

            migrationBuilder.DropColumn(
                name: "RepeatTypeTxt",
                table: "Carizos");
        }
    }
}
