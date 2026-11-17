using System;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    [DbContext(typeof(CRMContextDb))]
    [Migration("20261118130000_AddQualityControlContactHistories")]
    public partial class AddQualityControlContactHistories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QualityControlContactHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsLegacy = table.Column<bool>(type: "bit", nullable: false),
                    MobileAtAttempt = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DeletedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DeleteReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RestoredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RestoredById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RestoredByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
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
                    table.PrimaryKey("PK_QualityControlContactHistories", x => x.Id);
                    table.CheckConstraint("CK_QcContactHistories_Status", "[Status] IN (0, 1, 2)");
                    table.CheckConstraint("CK_QcContactHistories_Attempts", "[AttemptCount] >= 0");
                    table.ForeignKey(
                        name: "FK_QualityControlContactHistories_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QualityControlContactHistories_CustomerId_CreateDate",
                table: "QualityControlContactHistories",
                columns: new[] { "CustomerId", "CreateDate" });

            // Prior releases only stored the cumulative no-answer count and latest
            // contact outcome. Preserve them as LEGACY summaries; never fabricate
            // individual dates, times or representatives for earlier attempts.
            migrationBuilder.Sql(@"
                INSERT INTO dbo.QualityControlContactHistories
                (Id, CustomerId, Status, AttemptCount, IsLegacy, MobileAtAttempt,
                 Description, CreateDate, CreateDateTxt, ModifyDate, ModifyDateTxt,
                 IsDeleted, UserCreatorID, UserCreator, UserModifyID, UserModify, CommentText)
                SELECT NEWID(), c.CustomerId, 1,
                       CASE WHEN c.NoResponseAttempts > 0 THEN c.NoResponseAttempts ELSE 1 END,
                       1, NULL,
                       N'سابقه تجمیعی قدیمی؛ زمان و جزئیات تماس‌های جداگانه در نسخه قبلی ثبت نشده است.',
                       CASE WHEN c.Status = 2
                           THEN DATEADD(second, -1, COALESCE(c.ModifyDate, c.CreateDate))
                           ELSE COALESCE(c.LastNoResponseAt, c.ModifyDate, c.CreateDate) END,
                       NULL, NULL, NULL, 0,
                       c.UserCreatorID, c.UserCreator, c.UserModifyID, c.UserModify, NULL
                FROM dbo.QualityControlCustomerContacts AS c
                WHERE c.IsDeleted = 0 AND (c.NoResponseAttempts > 0 OR c.Status = 1);

                INSERT INTO dbo.QualityControlContactHistories
                (Id, CustomerId, Status, AttemptCount, IsLegacy, MobileAtAttempt,
                 Description, CreateDate, CreateDateTxt, ModifyDate, ModifyDateTxt,
                 IsDeleted, UserCreatorID, UserCreator, UserModifyID, UserModify, CommentText)
                SELECT NEWID(), c.CustomerId, 2, 0, 1, c.InvalidMobileAtMark,
                       N'شماره نامعتبر ثبت‌شده پیش از ایجاد تاریخچه تماس.',
                       COALESCE(c.ModifyDate, c.CreateDate), c.ModifyDateTxt,
                       NULL, NULL, 0, c.UserCreatorID, c.UserCreator,
                       c.UserModifyID, c.UserModify, NULL
                FROM dbo.QualityControlCustomerContacts AS c
                WHERE c.IsDeleted = 0 AND c.Status = 2;

                INSERT INTO dbo.QualityControlContactHistories
                (Id, CustomerId, Status, AttemptCount, IsLegacy, MobileAtAttempt,
                 Description, CreateDate, CreateDateTxt, ModifyDate, ModifyDateTxt,
                 IsDeleted, UserCreatorID, UserCreator, UserModifyID, UserModify, CommentText)
                SELECT NEWID(), c.CustomerId, 0, 0, 1, NULL,
                       N'وضعیت تماس پیش از ایجاد تاریخچه پاک شده بود.',
                       DATEADD(second, 1, COALESCE(c.ModifyDate, c.CreateDate)),
                       c.ModifyDateTxt,
                       NULL, NULL, 0, c.UserCreatorID, c.UserCreator,
                       c.UserModifyID, c.UserModify, NULL
                FROM dbo.QualityControlCustomerContacts AS c
                WHERE c.IsDeleted = 0 AND c.Status = 0 AND c.NoResponseAttempts > 0;

                UPDATE c
                SET NoResponseAttempts = 1
                FROM dbo.QualityControlCustomerContacts AS c
                WHERE c.IsDeleted = 0 AND c.Status = 1 AND c.NoResponseAttempts = 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "QualityControlContactHistories");
        }
    }
}
