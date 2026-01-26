using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up1404040301 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cus_Address",
                table: "PreFactors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cus_Mobile",
                table: "PreFactors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cus_PostalCode",
                table: "PreFactors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cus_Tel",
                table: "PreFactors",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cus_Address",
                table: "PreFactors");

            migrationBuilder.DropColumn(
                name: "Cus_Mobile",
                table: "PreFactors");

            migrationBuilder.DropColumn(
                name: "Cus_PostalCode",
                table: "PreFactors");

            migrationBuilder.DropColumn(
                name: "Cus_Tel",
                table: "PreFactors");
        }
    }
}
