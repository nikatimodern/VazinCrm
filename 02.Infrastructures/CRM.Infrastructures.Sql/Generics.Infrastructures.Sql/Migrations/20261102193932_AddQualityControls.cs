using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QualityControlQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ZeroAnswerText = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OneAnswerText = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_QualityControlQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QualityControls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_QualityControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityControls_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityControlAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QualityControlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Answer = table.Column<byte>(type: "tinyint", nullable: false),
                    QuestionTitleSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    QuestionTextSnapshot = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ZeroAnswerTextSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OneAnswerTextSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SortOrderSnapshot = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_QualityControlAnswers", x => x.Id);
                    table.CheckConstraint("CK_QualityControlAnswers_Answer", "[Answer] IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_QualityControlAnswers_QualityControlQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QualityControlQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityControlAnswers_QualityControls_QualityControlId",
                        column: x => x.QualityControlId,
                        principalTable: "QualityControls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "QualityControlQuestions",
                columns: new[] { "Id", "CommentText", "CreateDate", "CreateDateTxt", "IsActive", "IsDeleted", "ModifyDate", "ModifyDateTxt", "OneAnswerText", "QuestionText", "SortOrder", "Title", "UserCreator", "UserCreatorID", "UserModify", "UserModifyID", "ZeroAnswerText" },
                values: new object[,]
                {
                    { new Guid("45c326dd-4500-4cf5-94ce-000000000001"), "", new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "1405/08/11 00:00:00", true, false, new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "1405/08/11 00:00:00", "بله (دارای مغایرت)", "آیا در کیفیت محصول دریافتی (مثل ضخامت، جنس، چاپ، دوخت یا رنگ) مغایرتی با سفارشتان وجود داشت؟", 1, "مغایرت در کیفیت محصول", "System", "system", "System", "system", "خیر (سالم)" },
                    { new Guid("45c326dd-4500-4cf5-94ce-000000000002"), "", new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "1405/08/11 00:00:00", true, false, new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "1405/08/11 00:00:00", "بله (کسری/اضافه)", "آیا در تعداد کارتن‌ها، وزن بار یا متراژ تحویلی، مغایرت یا کسری داشتید؟", 2, "مغایرت در مقدار / وزن", "System", "system", "System", "system", "خیر (کامل)" },
                    { new Guid("45c326dd-4500-4cf5-94ce-000000000003"), "", new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "1405/08/11 00:00:00", true, false, new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "1405/08/11 00:00:00", "بله، تأخیر داشت", "آیا بار طبق زمان توافق‌شده تحویل شد یا تأخیر نامتعارف داشت؟", 3, "مغایرت در زمان تحویل", "System", "system", "System", "system", "خیر، به‌موقع بود" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_QualityControlAnswers_QualityControlId_QuestionId",
                table: "QualityControlAnswers",
                columns: new[] { "QualityControlId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityControlAnswers_QuestionId",
                table: "QualityControlAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityControls_DealId",
                table: "QualityControls",
                column: "DealId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QualityControlAnswers");

            migrationBuilder.DropTable(
                name: "QualityControlQuestions");

            migrationBuilder.DropTable(
                name: "QualityControls");
        }
    }
}
