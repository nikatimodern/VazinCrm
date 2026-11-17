using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    [DbContext(typeof(CRMContextDb))]
    [Migration("20261117193000_AllowUncheckedQualityControlItems")]
    public partial class AllowUncheckedQualityControlItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_QualityControlItemReviews_Status",
                table: "QualityControlItemReviews");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QualityControlItemReviews_Status",
                table: "QualityControlItemReviews",
                sql: "[Status] IN (0, 1, 2)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverting must not fail for reviews currently marked unchecked.
            migrationBuilder.Sql("DELETE FROM [QualityControlItemAnswers] WHERE [QualityControlItemReviewId] IN (SELECT [Id] FROM [QualityControlItemReviews] WHERE [Status] = 0)");
            migrationBuilder.Sql("DELETE FROM [QualityControlItemReviews] WHERE [Status] = 0");
            migrationBuilder.DropCheckConstraint(
                name: "CK_QualityControlItemReviews_Status",
                table: "QualityControlItemReviews");
            migrationBuilder.AddCheckConstraint(
                name: "CK_QualityControlItemReviews_Status",
                table: "QualityControlItemReviews",
                sql: "[Status] IN (1, 2)");
        }
    }
}
