using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up14040201 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Accepted",
                table: "Factors",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FactorTowStep",
                table: "ASettings",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Accepted",
                table: "Factors");

            migrationBuilder.DropColumn(
                name: "FactorTowStep",
                table: "ASettings");
        }
    }
}
