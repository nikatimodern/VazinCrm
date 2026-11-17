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
            // Do not silently delete historical answers or misclassify unchecked reviews.
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM [QualityControlItemReviews] WHERE [Status] = 0)
                THROW 51000, 'Cannot roll back quality-control status constraint while unchecked reviews exist.', 1;");
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
