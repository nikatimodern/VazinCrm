using System;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations;

[DbContext(typeof(CRMContextDb))]
[Migration("20261119220500_AddQualityControlCustomerNotes")]
public partial class AddQualityControlCustomerNotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "QualityControlCustomerNotes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                Source = table.Column<byte>(type: "tinyint", nullable: false),
                CallRecordingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LegacyQualityControlId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                OriginalTranscript = table.Column<string>(type: "nvarchar(max)", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                UserCreatorID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                UserCreator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                UserModifyID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                UserModify = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CommentText = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QualityControlCustomerNotes", x => x.Id);
                table.ForeignKey(
                    name: "FK_QualityControlCustomerNotes_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers", principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_QualityControlCustomerNotes_CustomerId_CreateDate",
            table: "QualityControlCustomerNotes",
            columns: new[] { "CustomerId", "CreateDate" });

        migrationBuilder.CreateIndex(
            name: "IX_QualityControlCustomerNotes_CallRecordingId",
            table: "QualityControlCustomerNotes",
            column: "CallRecordingId", unique: true,
            filter: "[CallRecordingId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_QualityControlCustomerNotes_LegacyQualityControlId",
            table: "QualityControlCustomerNotes",
            column: "LegacyQualityControlId", unique: true,
            filter: "[LegacyQualityControlId] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "QualityControlCustomerNotes");
    }
}
