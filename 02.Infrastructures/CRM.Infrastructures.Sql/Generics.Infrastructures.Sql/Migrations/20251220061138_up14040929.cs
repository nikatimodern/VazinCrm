using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up14040929 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrencyType",
                table: "PreFactors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyTypeTxt",
                table: "PreFactors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyType",
                table: "Factors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyTypeTxt",
                table: "Factors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyType",
                table: "Accountancies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyTypeTxt",
                table: "Accountancies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyType",
                table: "PreFactors");

            migrationBuilder.DropColumn(
                name: "CurrencyTypeTxt",
                table: "PreFactors");

            migrationBuilder.DropColumn(
                name: "CurrencyType",
                table: "Factors");

            migrationBuilder.DropColumn(
                name: "CurrencyTypeTxt",
                table: "Factors");

            migrationBuilder.DropColumn(
                name: "CurrencyType",
                table: "Accountancies");

            migrationBuilder.DropColumn(
                name: "CurrencyTypeTxt",
                table: "Accountancies");
        }
    }
}
