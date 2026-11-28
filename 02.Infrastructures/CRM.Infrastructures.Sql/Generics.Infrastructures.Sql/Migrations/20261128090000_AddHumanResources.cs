using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations.CRMContextDbMigrations
{
    // Manually authored against the checked-in CRM snapshot. No existing table is changed in Up.
    public partial class AddHumanResources : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HrEmployeeProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PersonnelCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    UserCreator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserCreatorID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModify = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModifyID = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrEmployeeProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HrDepartments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    UserCreator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserCreatorID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModify = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModifyID = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrDepartments", x => x.Id);
                    table.CheckConstraint("CK_HrDepartments_Parent", "[ParentId] IS NULL OR [ParentId] <> [Id]");
                    table.ForeignKey(
                        name: "FK_HrDepartments_HrDepartments_ParentId",
                        column: x => x.ParentId,
                        principalTable: "HrDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HrJobPositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LegacyPositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UserCreator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserCreatorID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModify = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModifyID = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrJobPositions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HrEmploymentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UserCreator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserCreatorID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModify = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModifyID = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrEmploymentRecords", x => x.Id);
                    table.CheckConstraint("CK_HrEmploymentRecords_Dates", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
                    table.ForeignKey(
                        name: "FK_HrEmploymentRecords_HrEmployeeProfiles_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "HrEmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HrEmployeeAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    ManagerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    UserCreator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserCreatorID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModify = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModifyID = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrEmployeeAssignments", x => x.Id);
                    table.CheckConstraint("CK_HrEmployeeAssignments_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.CheckConstraint("CK_HrEmployeeAssignments_Manager", "[ManagerEmployeeId] IS NULL OR [ManagerEmployeeId] <> [EmployeeId]");
                    table.ForeignKey(
                        name: "FK_HrEmployeeAssignments_HrDepartments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "HrDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrEmployeeAssignments_HrEmployeeProfiles_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "HrEmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrEmployeeAssignments_HrEmployeeProfiles_ManagerEmployeeId",
                        column: x => x.ManagerEmployeeId,
                        principalTable: "HrEmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrEmployeeAssignments_HrJobPositions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "HrJobPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HrEmploymentContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgreementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Term = table.Column<int>(type: "int", nullable: false),
                    TerminatedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    UserCreator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserCreatorID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModify = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModifyID = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrEmploymentContracts", x => x.Id);
                    table.UniqueConstraint("AK_HrEmploymentContracts_EmployeeId_Id", x => new { x.EmployeeId, x.Id });
                    table.CheckConstraint("CK_HrContracts_Term", "([Term] = 1 AND [EndDate] IS NOT NULL AND [EndDate] >= [StartDate]) OR ([Term] = 2 AND [EndDate] IS NULL)");
                    table.CheckConstraint("CK_HrContracts_Predecessor", "[PreviousContractId] IS NULL OR [PreviousContractId] <> [Id]");
                    table.CheckConstraint("CK_HrContracts_Status", "[Status] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_HrContracts_Termination", "([Status] = 4 AND [TerminatedOn] IS NOT NULL AND [TerminatedOn] >= [StartDate] AND ([EndDate] IS NULL OR [TerminatedOn] <= [EndDate])) OR ([Status] <> 4 AND [TerminatedOn] IS NULL)");
                    table.CheckConstraint("CK_HrContracts_Approval", "([Status] IN (2, 4) AND [ApprovedByUserId] IS NOT NULL AND [ApprovedAtUtc] IS NOT NULL) OR ([Status] IN (1, 3) AND [ApprovedByUserId] IS NULL AND [ApprovedAtUtc] IS NULL)");
                    table.CheckConstraint("CK_HrContracts_Resolution", "([Status] IN (3, 4) AND [ResolvedByUserId] IS NOT NULL AND [ResolvedAtUtc] IS NOT NULL AND [ResolutionReason] IS NOT NULL) OR ([Status] IN (1, 2) AND [ResolvedByUserId] IS NULL AND [ResolvedAtUtc] IS NULL AND [ResolutionReason] IS NULL)");
                    table.ForeignKey(
                        name: "FK_HrEmploymentContracts_HrEmployeeProfiles_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "HrEmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrEmploymentContracts_HrEmploymentContracts_EmployeeId_PreviousContractId",
                        columns: x => new { x.EmployeeId, x.PreviousContractId },
                        principalTable: "HrEmploymentContracts",
                        principalColumns: new[] { "EmployeeId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeProfiles_AppUserId",
                table: "HrEmployeeProfiles",
                column: "AppUserId",
                unique: true,
                filter: "[AppUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeProfiles_PersonnelCode",
                table: "HrEmployeeProfiles",
                column: "PersonnelCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HrDepartments_Code",
                table: "HrDepartments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HrDepartments_ParentId",
                table: "HrDepartments",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_HrJobPositions_Code",
                table: "HrJobPositions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HrEmploymentRecords_EmployeeId_StartDate",
                table: "HrEmploymentRecords",
                columns: new[] { "EmployeeId", "StartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeAssignments_DepartmentId",
                table: "HrEmployeeAssignments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeAssignments_EmployeeId_EffectiveFrom",
                table: "HrEmployeeAssignments",
                columns: new[] { "EmployeeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeAssignments_ManagerEmployeeId",
                table: "HrEmployeeAssignments",
                column: "ManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeAssignments_PositionId",
                table: "HrEmployeeAssignments",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmploymentContracts_ContractNumber",
                table: "HrEmploymentContracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HrEmploymentContracts_EmployeeId_PreviousContractId",
                table: "HrEmploymentContracts",
                columns: new[] { "EmployeeId", "PreviousContractId" });

            migrationBuilder.CreateIndex(
                name: "IX_HrEmploymentContracts_EmployeeId_StartDate",
                table: "HrEmploymentContracts",
                columns: new[] { "EmployeeId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_HrEmploymentContracts_Status_EndDate_Id",
                table: "HrEmploymentContracts",
                columns: new[] { "Status", "EndDate", "Id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback drops HR tables and any HR data they contain. Back up before rollback.
            migrationBuilder.DropTable(name: "HrEmploymentContracts");
            migrationBuilder.DropTable(name: "HrEmployeeAssignments");
            migrationBuilder.DropTable(name: "HrEmploymentRecords");
            migrationBuilder.DropTable(name: "HrJobPositions");
            migrationBuilder.DropTable(name: "HrDepartments");
            migrationBuilder.DropTable(name: "HrEmployeeProfiles");
        }
    }
}
