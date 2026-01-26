using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up1404050601 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "logSystems",
                newName: "ParentEntityId");

            migrationBuilder.AddColumn<string>(
                name: "ParentEntityName",
                table: "logSystems",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentEntityName",
                table: "logSystems");

            migrationBuilder.RenameColumn(
                name: "ParentEntityId",
                table: "logSystems",
                newName: "ParentId");
        }
    }
}
