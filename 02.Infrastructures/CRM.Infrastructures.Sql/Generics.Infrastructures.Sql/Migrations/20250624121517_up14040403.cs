using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up14040403 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CartableStep",
                table: "PreFactors");

            migrationBuilder.RenameColumn(
                name: "CartableUserTxt",
                table: "PreFactors",
                newName: "Cus_ShenaseMeli");

            migrationBuilder.RenameColumn(
                name: "CartableUserId",
                table: "PreFactors",
                newName: "Cus_NumberRegister");

            migrationBuilder.RenameColumn(
                name: "CartableRolTxt",
                table: "PreFactors",
                newName: "Cus_EghtesadiCode");

            migrationBuilder.RenameColumn(
                name: "CartableRolID",
                table: "PreFactors",
                newName: "AgentMeliCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Cus_ShenaseMeli",
                table: "PreFactors",
                newName: "CartableUserTxt");

            migrationBuilder.RenameColumn(
                name: "Cus_NumberRegister",
                table: "PreFactors",
                newName: "CartableUserId");

            migrationBuilder.RenameColumn(
                name: "Cus_EghtesadiCode",
                table: "PreFactors",
                newName: "CartableRolTxt");

            migrationBuilder.RenameColumn(
                name: "AgentMeliCode",
                table: "PreFactors",
                newName: "CartableRolID");

            migrationBuilder.AddColumn<int>(
                name: "CartableStep",
                table: "PreFactors",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
