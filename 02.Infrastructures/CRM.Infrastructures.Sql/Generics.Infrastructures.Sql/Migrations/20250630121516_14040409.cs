using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class _14040409 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColumnName",
                table: "logSystems");

            migrationBuilder.DropColumn(
                name: "NewValue",
                table: "logSystems");

            migrationBuilder.RenameColumn(
                name: "TableName",
                table: "logSystems",
                newName: "OldValue");

            migrationBuilder.RenameColumn(
                name: "TableId",
                table: "logSystems",
                newName: "EntityName");

            migrationBuilder.RenameColumn(
                name: "OrginalValue",
                table: "logSystems",
                newName: "EntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OldValue",
                table: "logSystems",
                newName: "TableName");

            migrationBuilder.RenameColumn(
                name: "EntityName",
                table: "logSystems",
                newName: "TableId");

            migrationBuilder.RenameColumn(
                name: "EntityId",
                table: "logSystems",
                newName: "OrginalValue");

            migrationBuilder.AddColumn<string>(
                name: "ColumnName",
                table: "logSystems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewValue",
                table: "logSystems",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
