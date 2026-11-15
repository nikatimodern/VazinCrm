using System;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace CRM.Infrastructures.Sql.Migrations.UserContextDbMigrations;

[DbContext(typeof(UserContextDb))]
[Migration("20261115220000_AddAccessHistoryEntries")]
public partial class AddAccessHistoryEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AccessHistoryEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ChangeCount = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AccessHistoryEntries", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_AccessHistoryEntries_CreatedAtUtc",
            table: "AccessHistoryEntries", column: "CreatedAtUtc");
        migrationBuilder.CreateIndex(name: "IX_AccessHistoryEntries_Kind_CreatedAtUtc",
            table: "AccessHistoryEntries", columns: new[] { "Kind", "CreatedAtUtc" });
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "AccessHistoryEntries");
}
