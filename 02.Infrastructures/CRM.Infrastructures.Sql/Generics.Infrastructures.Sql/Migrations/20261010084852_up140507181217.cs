using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140507181217 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalSoftwares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalSoftwares", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalSoftwareId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MapType = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalMappings_ExternalSoftwares_ExternalSoftwareId",
                        column: x => x.ExternalSoftwareId,
                        principalTable: "ExternalSoftwares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalMappings_ExternalSoftwareId",
                table: "ExternalMappings",
                column: "ExternalSoftwareId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalMappings");
            
            migrationBuilder.DropTable(
                name: "ExternalSoftwares");

        }
    }
}
