using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507131457 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PatternCode",
                table: "SentSmses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatternCodeTxt",
                table: "SentSmses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatternCode",
                table: "SentSmses");

            migrationBuilder.DropColumn(
                name: "PatternCodeTxt",
                table: "SentSmses");
        }
    }
}
