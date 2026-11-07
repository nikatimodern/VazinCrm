using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations
{
    /// <inheritdoc />
    public partial class AddReleaseNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReleaseNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Audience = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsImportant = table.Column<bool>(type: "bit", nullable: false),
                    ShowPopup = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseNoteItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleaseNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    LearnMoreUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseNoteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseNoteItems_ReleaseNotes_ReleaseNoteId",
                        column: x => x.ReleaseNoteId,
                        principalTable: "ReleaseNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseNoteRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleaseNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseNoteRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseNoteRoles_ReleaseNotes_ReleaseNoteId",
                        column: x => x.ReleaseNoteId,
                        principalTable: "ReleaseNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserReleaseNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReleaseNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeenDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReleaseNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReleaseNotes_ReleaseNotes_ReleaseNoteId",
                        column: x => x.ReleaseNoteId,
                        principalTable: "ReleaseNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseNoteItems_ReleaseNoteId_SortOrder",
                table: "ReleaseNoteItems",
                columns: new[] { "ReleaseNoteId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseNoteRoles_ReleaseNoteId_RoleName",
                table: "ReleaseNoteRoles",
                columns: new[] { "ReleaseNoteId", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseNotes_Status_PublishDate",
                table: "ReleaseNotes",
                columns: new[] { "Status", "PublishDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseNotes_Version",
                table: "ReleaseNotes",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserReleaseNotes_ReleaseNoteId",
                table: "UserReleaseNotes",
                column: "ReleaseNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReleaseNotes_UserId_ReleaseNoteId",
                table: "UserReleaseNotes",
                columns: new[] { "UserId", "ReleaseNoteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReleaseNoteItems");

            migrationBuilder.DropTable(
                name: "ReleaseNoteRoles");

            migrationBuilder.DropTable(
                name: "UserReleaseNotes");

            migrationBuilder.DropTable(
                name: "ReleaseNotes");
        }
    }
}
