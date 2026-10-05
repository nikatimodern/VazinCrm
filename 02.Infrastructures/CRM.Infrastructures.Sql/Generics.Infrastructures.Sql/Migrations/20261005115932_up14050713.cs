using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up14050713 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PatternType",
                table: "SentSmses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatternTypeTxt",
                table: "SentSmses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatternType",
                table: "SentSmses");

            migrationBuilder.DropColumn(
                name: "PatternTypeTxt",
                table: "SentSmses");
        }
    }
}
