using System;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations;

[DbContext(typeof(CRMContextDb))]
[Migration("20261107190000_AddSalesTargets")]
public partial class AddSalesTargets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TargetMetrics",
            columns: table => new
            {
                Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Kind = table.Column<byte>(type: "tinyint", nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_TargetMetrics", x => x.Code));

        migrationBuilder.CreateTable(
            name: "TargetPlans",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Year = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<byte>(type: "tinyint", nullable: false),
                Version = table.Column<long>(type: "bigint", nullable: false),
                CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                ModifiedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                FinalizedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_TargetPlans", x => x.Id));

        migrationBuilder.CreateTable(
            name: "TargetValues",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MetricCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                Month = table.Column<int>(type: "int", nullable: false),
                Value = table.Column<decimal>(type: "decimal(28,2)", precision: 28, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TargetValues", x => x.Id);
                table.CheckConstraint("CK_TargetValues_Month", "[Month] >= 0 AND [Month] <= 12");
                table.ForeignKey("FK_TargetValues_TargetMetrics_MetricCode", x => x.MetricCode, "TargetMetrics", "Code", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TargetValues_TargetPlans_PlanId", x => x.PlanId, "TargetPlans", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TargetAssignments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MetricCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                Month = table.Column<int>(type: "int", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                Value = table.Column<decimal>(type: "decimal(28,2)", precision: 28, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TargetAssignments", x => x.Id);
                table.CheckConstraint("CK_TargetAssignments_Month", "[Month] >= 1 AND [Month] <= 12");
                table.ForeignKey("FK_TargetAssignments_TargetMetrics_MetricCode", x => x.MetricCode, "TargetMetrics", "Code", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TargetAssignments_TargetPlans_PlanId", x => x.PlanId, "TargetPlans", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TargetPlanAudits",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Version = table.Column<long>(type: "bigint", nullable: false),
                Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                AtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TargetPlanAudits", x => x.Id);
                table.ForeignKey("FK_TargetPlanAudits_TargetPlans_PlanId", x => x.PlanId, "TargetPlans", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_TargetMetrics_SortOrder", "TargetMetrics", "SortOrder");
        migrationBuilder.CreateIndex("IX_TargetPlans_Year", "TargetPlans", "Year", unique: true);
        migrationBuilder.CreateIndex("IX_TargetValues_MetricCode", "TargetValues", "MetricCode");
        migrationBuilder.CreateIndex("IX_TargetValues_PlanId_MetricCode_Month", "TargetValues", new[] { "PlanId", "MetricCode", "Month" }, unique: true);
        migrationBuilder.CreateIndex("IX_TargetAssignments_MetricCode", "TargetAssignments", "MetricCode");
        migrationBuilder.CreateIndex("IX_TargetAssignments_PlanId_MetricCode_Month_UserId", "TargetAssignments", new[] { "PlanId", "MetricCode", "Month", "UserId" }, unique: true);
        migrationBuilder.CreateIndex("IX_TargetPlanAudits_PlanId_Version", "TargetPlanAudits", new[] { "PlanId", "Version" }, unique: true);

        migrationBuilder.InsertData(
            table: "TargetMetrics",
            columns: new[] { "Code", "Title", "Unit", "Kind", "SortOrder", "IsActive" },
            values: new object[,]
            {
                { "sales_amount", "فروش ریالی", "ریال", (byte)0, 1, true },
                { "new_customer_count", "تعداد مشتری جدید", "نفر", (byte)1, 2, true },
                { "new_customer_sales", "مبلغ فروش مشتری جدید", "ریال", (byte)0, 3, true },
                { "conversion_rate", "نرخ تبدیل", "درصد", (byte)2, 4, true },
                { "reactivated_customer_count", "تعداد مشتری بازجذب‌شده", "نفر", (byte)1, 5, true },
                { "reactivated_customer_sales", "مبلغ فروش مشتری بازجذب‌شده", "ریال", (byte)0, 6, true }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("TargetAssignments");
        migrationBuilder.DropTable("TargetPlanAudits");
        migrationBuilder.DropTable("TargetValues");
        migrationBuilder.DropTable("TargetPlans");
        migrationBuilder.DropTable("TargetMetrics");
    }
}
