using Microsoft.EntityFrameworkCore.Migrations;

namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations
{
    public partial class init2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "BaseAdmins",
                newName: "PersianTitle");

            migrationBuilder.AddColumn<string>(
                name: "LatinTitle",
                table: "BaseAdmins",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatinTitle",
                table: "BaseAdmins");

            migrationBuilder.RenameColumn(
                name: "PersianTitle",
                table: "BaseAdmins",
                newName: "Title");
        }
    }
}
