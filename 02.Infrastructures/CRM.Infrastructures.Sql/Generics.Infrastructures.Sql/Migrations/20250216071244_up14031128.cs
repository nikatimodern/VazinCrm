using Microsoft.EntityFrameworkCore.Migrations;

namespace CRM.Infrastructures.Sql.Migrations
{
    public partial class up14031128 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Register",
                table: "Accountancies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Vosoled",
                table: "Accountancies",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Register",
                table: "Accountancies");

            migrationBuilder.DropColumn(
                name: "Vosoled",
                table: "Accountancies");
        }
    }
}
