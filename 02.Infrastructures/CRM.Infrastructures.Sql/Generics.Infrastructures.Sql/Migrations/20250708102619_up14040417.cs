using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up14040417 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OldValue",
                table: "logSystems",
                newName: "ChangeDetails");

            migrationBuilder.RenameColumn(
                name: "ChangeBy",
                table: "logSystems",
                newName: "ChangedBy");

            migrationBuilder.RenameColumn(
                name: "ActionTxt",
                table: "logSystems",
                newName: "ActionTypeTxt");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "logSystems",
                newName: "ActionType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChangeDetails",
                table: "logSystems",
                newName: "OldValue");

            migrationBuilder.RenameColumn(
                name: "ChangedBy",
                table: "logSystems",
                newName: "ChangeBy");

            migrationBuilder.RenameColumn(
                name: "ActionTypeTxt",
                table: "logSystems",
                newName: "ActionTxt");

            migrationBuilder.RenameColumn(
                name: "ActionType",
                table: "logSystems",
                newName: "Action");
        }
    }
}
