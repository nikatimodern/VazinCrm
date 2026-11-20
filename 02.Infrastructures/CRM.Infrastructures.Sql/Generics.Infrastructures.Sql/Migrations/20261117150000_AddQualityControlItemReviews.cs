using System;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    [DbContext(typeof(CRMContextDb))]
    [Migration("20261117150000_AddQualityControlItemReviews")]
    public partial class AddQualityControlItemReviews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QualityControlItemReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FactorItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
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
                    table.PrimaryKey("PK_QualityControlItemReviews", x => x.Id);
                    table.CheckConstraint("CK_QualityControlItemReviews_Status", "[Status] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_QualityControlItemReviews_FactorItems_FactorItemId",
                        column: x => x.FactorItemId,
                        principalTable: "FactorItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityControlItemAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QualityControlItemReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_QualityControlItemAnswers", x => x.Id);
                    table.CheckConstraint("CK_QualityControlItemAnswers_Answer", "[Answer] IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_QualityControlItemAnswers_QualityControlItemReviews_QualityControlItemReviewId",
                        column: x => x.QualityControlItemReviewId,
                        principalTable: "QualityControlItemReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualityControlItemAnswers_QualityControlQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QualityControlQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QualityControlItemReviews_FactorItemId",
                table: "QualityControlItemReviews",
                column: "FactorItemId", unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_QualityControlItemAnswers_QualityControlItemReviewId_QuestionId",
                table: "QualityControlItemAnswers",
                columns: new[] { "QualityControlItemReviewId", "QuestionId" }, unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_QualityControlItemAnswers_QuestionId",
                table: "QualityControlItemAnswers", column: "QuestionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "QualityControlItemAnswers");
            migrationBuilder.DropTable(name: "QualityControlItemReviews");
        }
    }
}
