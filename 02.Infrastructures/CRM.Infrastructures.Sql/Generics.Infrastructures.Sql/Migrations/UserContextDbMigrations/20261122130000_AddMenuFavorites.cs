using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations;

[DbContext(typeof(UserContextDb))]
[Migration("20261122130000_AddMenuFavorites")]
public partial class AddMenuFavorites : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MenuFavorites",
            columns: table => new
            {
                UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                MenuId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_MenuFavorites", x => new { x.UserId, x.MenuId }));
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "MenuFavorites");
}
