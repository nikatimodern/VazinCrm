using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up1404042104 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TaminUnitAmount",
                table: "PreFactorItems",
                type: "decimal(20,0)",
                precision: 20,
                scale: 0,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaminTotalAmount",
                table: "PreFactorItems",
                type: "decimal(20,0)",
                precision: 20,
                scale: 0,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TaminUnitAmount",
                table: "PreFactorItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldPrecision: 20,
                oldScale: 0,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaminTotalAmount",
                table: "PreFactorItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldPrecision: 20,
                oldScale: 0,
                oldNullable: true);
        }
    }
}
