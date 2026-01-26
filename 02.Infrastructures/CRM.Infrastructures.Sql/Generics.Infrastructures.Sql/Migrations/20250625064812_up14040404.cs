using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up14040404 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgentMeliCode",
                table: "Factors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cus_Address",
                table: "Factors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cus_EghtesadiCode",
                table: "Factors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cus_Mobile",
                table: "Factors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cus_NumberRegister",
                table: "Factors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cus_PostalCode",
                table: "Factors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cus_ShenaseMeli",
                table: "Factors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cus_Tel",
                table: "Factors",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgentMeliCode",
                table: "Factors");

            migrationBuilder.DropColumn(
                name: "Cus_Address",
                table: "Factors");

            migrationBuilder.DropColumn(
                name: "Cus_EghtesadiCode",
                table: "Factors");

            migrationBuilder.DropColumn(
                name: "Cus_Mobile",
                table: "Factors");

            migrationBuilder.DropColumn(
                name: "Cus_NumberRegister",
                table: "Factors");

            migrationBuilder.DropColumn(
                name: "Cus_PostalCode",
                table: "Factors");

            migrationBuilder.DropColumn(
                name: "Cus_ShenaseMeli",
                table: "Factors");

            migrationBuilder.DropColumn(
                name: "Cus_Tel",
                table: "Factors");
        }
    }
}
