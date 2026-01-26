using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructures.Sql.Migrations
{
    /// <inheritdoc />
    public partial class up1404041802 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropPrimaryKey(
            //    name: "PK_UserCustomers",
            //    table: "UserCustomers");

            //migrationBuilder.AlterColumn<Guid>(
            //    name: "Id",
            //    table: "UserCustomers",
            //    type: "uniqueidentifier",
            //    nullable: false,
            //    defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            //    oldClrType: typeof(Guid),
            //    oldType: "uniqueidentifier",
            //    oldNullable: true);

            //migrationBuilder.AddPrimaryKey(
            //    name: "PK_UserCustomers",
            //    table: "UserCustomers",
            //    column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropPrimaryKey(
            //    name: "PK_UserCustomers",
            //    table: "UserCustomers");

            //migrationBuilder.AlterColumn<Guid>(
            //    name: "Id",
            //    table: "UserCustomers",
            //    type: "uniqueidentifier",
            //    nullable: true,
            //    oldClrType: typeof(Guid),
            //    oldType: "uniqueidentifier");

            //migrationBuilder.AddPrimaryKey(
            //    name: "PK_UserCustomers",
            //    table: "UserCustomers",
            //    columns: new[] { "UserId", "CustomerId" });
        }
    }
}
