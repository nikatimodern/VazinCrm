using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up140411261246 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_Contracts_contractId",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_contractId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "contractId",
                table: "Attachments");

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FromAccount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToAccount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupplyFactorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestAmounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocType = table.Column<int>(type: "int", nullable: true),
                    DocTypeTXT = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_RequestAmounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplyFactors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FactorSerial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FactorDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FactorDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_SupplyFactors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentSupplyFactors",
                columns: table => new
                {
                    SupplyFactorID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSupplyFactors", x => new { x.PaymentID, x.SupplyFactorID });
                    table.ForeignKey(
                        name: "FK_PaymentSupplyFactors_Payments_PaymentID",
                        column: x => x.PaymentID,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentSupplyFactors_SupplyFactors_SupplyFactorID",
                        column: x => x.SupplyFactorID,
                        principalTable: "SupplyFactors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestAmountSupplyFactors",
                columns: table => new
                {
                    RequestAmountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplyFactorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAmountSupplyFactors", x => new { x.SupplyFactorId, x.RequestAmountId });
                    table.ForeignKey(
                        name: "FK_RequestAmountSupplyFactors_RequestAmounts_RequestAmountId",
                        column: x => x.RequestAmountId,
                        principalTable: "RequestAmounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestAmountSupplyFactors_SupplyFactors_SupplyFactorId",
                        column: x => x.SupplyFactorId,
                        principalTable: "SupplyFactors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplyFactorOrderHeads",
                columns: table => new
                {
                    SupplyFactorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderHeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifyDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyFactorOrderHeads", x => new { x.SupplyFactorId, x.OrderHeadId });
                    table.ForeignKey(
                        name: "FK_SupplyFactorOrderHeads_OrderHeads_OrderHeadId",
                        column: x => x.OrderHeadId,
                        principalTable: "OrderHeads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplyFactorOrderHeads_SupplyFactors_SupplyFactorId",
                        column: x => x.SupplyFactorId,
                        principalTable: "SupplyFactors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSupplyFactors_SupplyFactorID",
                table: "PaymentSupplyFactors",
                column: "SupplyFactorID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestAmountSupplyFactors_RequestAmountId",
                table: "RequestAmountSupplyFactors",
                column: "RequestAmountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyFactorOrderHeads_OrderHeadId",
                table: "SupplyFactorOrderHeads",
                column: "OrderHeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentSupplyFactors");

            migrationBuilder.DropTable(
                name: "RequestAmountSupplyFactors");

            migrationBuilder.DropTable(
                name: "SupplyFactorOrderHeads");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "RequestAmounts");

            migrationBuilder.DropTable(
                name: "SupplyFactors");

            migrationBuilder.AddColumn<Guid>(
                name: "contractId",
                table: "Attachments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_contractId",
                table: "Attachments",
                column: "contractId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Contracts_contractId",
                table: "Attachments",
                column: "contractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
