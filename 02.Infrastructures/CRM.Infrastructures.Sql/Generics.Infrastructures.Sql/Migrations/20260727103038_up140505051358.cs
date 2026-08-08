using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140505051358 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTransferred",
                table: "Accountancies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferredTo",
                table: "Accountancies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTransferred",
                table: "Accountancies");

            migrationBuilder.DropColumn(
                name: "TransferredTo",
                table: "Accountancies");
        }
    }
}
