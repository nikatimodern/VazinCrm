using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up14040701 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountByTamin",
                table: "ASettings");

            migrationBuilder.AddColumn<int>(
                name: "SetAmountByRoleType",
                table: "ASettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SetAmountByRoleType",
                table: "ASettings");

            migrationBuilder.AddColumn<bool>(
                name: "AmountByTamin",
                table: "ASettings",
                type: "bit",
                nullable: true);
        }
    }
}
