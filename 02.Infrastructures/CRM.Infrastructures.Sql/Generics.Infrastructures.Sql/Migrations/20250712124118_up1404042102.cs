using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up1404042102 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TaminTotalAmount",
                table: "PreFactorItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaminUnitAmount",
                table: "PreFactorItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaminTotalAmount",
                table: "FactorItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaminUnitAmount",
                table: "FactorItems",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaminTotalAmount",
                table: "PreFactorItems");

            migrationBuilder.DropColumn(
                name: "TaminUnitAmount",
                table: "PreFactorItems");

            migrationBuilder.DropColumn(
                name: "TaminTotalAmount",
                table: "FactorItems");

            migrationBuilder.DropColumn(
                name: "TaminUnitAmount",
                table: "FactorItems");
        }
    }
}
