using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CRM.Infrastructures.Sql.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ASettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttAcceptHanding = table.Column<bool>(type: "bit", nullable: false),
                    AttPfPayment = table.Column<bool>(type: "bit", nullable: false),
                    AttFPayment = table.Column<bool>(type: "bit", nullable: false),
                    SmallOffice = table.Column<bool>(type: "bit", nullable: false),
                    Sepidar = table.Column<bool>(type: "bit", nullable: true),
                    CrmFactors = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BaseDescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dir = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Official = table.Column<bool>(type: "bit", nullable: true),
                    Type = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseDescriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BaseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeCode = table.Column<int>(type: "int", nullable: false),
                    TypeTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Carizos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskDesc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskRemindStatus = table.Column<bool>(type: "bit", nullable: false),
                    RemindType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemindDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemindDateM = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ColorTask = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Carizos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyBrandName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShenaseMeli = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EghtesadiCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberRegister = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerCode = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Juridical = table.Column<byte>(type: "tinyint", nullable: false),
                    Sex = table.Column<byte>(type: "tinyint", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EghtesadiCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberRegister = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShenaseMeli = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgentName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgentMeliCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resposibility = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Internal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkSenf = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkSenfTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkSenfDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcquaintanceWay = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcquaintanceWayTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateSort = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateSortTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Accepted = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExtraData",
                columns: table => new
                {
                    ExtraDataID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityTypeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityRef = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtraData", x => x.ExtraDataID);
                });

            migrationBuilder.CreateTable(
                name: "Invoice",
                columns: table => new
                {
                    InvoiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<int>(type: "int", nullable: false),
                    CustomerPartyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerEconomicCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerIdentificationCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerZipCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartyAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Addition = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PriceInBaseCurrency = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CustomerPartyRef = table.Column<int>(type: "int", nullable: false),
                    QuotationNumber = table.Column<int>(type: "int", nullable: true),
                    QuotationRef = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoice", x => x.InvoiceId);
                });

            migrationBuilder.CreateTable(
                name: "logSystems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TableId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TableName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColumnName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrginalValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTimeTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logSystems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quotation",
                columns: table => new
                {
                    QuotationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerPartyRef = table.Column<int>(type: "int", nullable: false),
                    CustomerPartyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotation", x => x.QuotationId);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportType = table.Column<int>(type: "int", nullable: false),
                    ReportTypeTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepTel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerCode = table.Column<int>(type: "int", nullable: false),
                    CustomerTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegTimeTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RemindDays",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Day = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    value = table.Column<bool>(type: "bit", nullable: false),
                    CarizoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemindDays", x => x.id);
                    table.ForeignKey(
                        name: "FK_RemindDays_Carizos_CarizoId",
                        column: x => x.CarizoId,
                        principalTable: "Carizos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Filename = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Filetype = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fileExten = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    file = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyFile_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Accountancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceType = table.Column<int>(type: "int", nullable: false),
                    InvoiceTypeTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayType = table.Column<int>(type: "int", nullable: false),
                    PayTypeTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccType = table.Column<int>(type: "int", nullable: false),
                    AccTypeTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    credit = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: true),
                    debit = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: true),
                    discount = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: true),
                    tax = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: true),
                    taxPercent = table.Column<int>(type: "int", nullable: true),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    dateSh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pfid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    pfSerialNumber = table.Column<long>(type: "bigint", nullable: true),
                    fid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    fSerialNumber = table.Column<long>(type: "bigint", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNum = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstAcc = table.Column<bool>(type: "bit", nullable: false),
                    ToAccount = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Accountancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accountancies_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Employer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contractor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fromDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fromDateSh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    toDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    toDateSh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderHeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Neme = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Customer = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Prefactor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrefactorSer = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_OrderHeads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderHeads_Customers_Customer",
                        column: x => x.Customer,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreFactors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SumTotalAmount = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: false),
                    SumTotalAmountTax = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: false),
                    PercentTax = table.Column<int>(type: "int", nullable: false),
                    TotalTax = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: false),
                    ReciveAmount = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: false),
                    Date = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateMiladi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidityDade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidityDateMiladi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SerialNumber = table.Column<int>(type: "int", nullable: false),
                    CountNum = table.Column<int>(type: "int", nullable: false),
                    OfficialType = table.Column<byte>(type: "tinyint", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Customer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgentName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CartableRolID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CartableRolTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CartableUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CartableUserTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CartableStep = table.Column<int>(type: "int", nullable: false),
                    ExpireValidateDesc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrePftype = table.Column<int>(type: "int", nullable: true),
                    Pftype = table.Column<int>(type: "int", nullable: false),
                    PftypeTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PftypeChangeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PftypeChangeDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Accepted = table.Column<bool>(type: "bit", nullable: false),
                    AcceptedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Signature = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    SignatureDes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Export = table.Column<bool>(type: "bit", nullable: true),
                    UserTaminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserTaminTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PreFactors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreFactors_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCustomers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCustomers", x => new { x.UserId, x.CustomerId });
                    table.ForeignKey(
                        name: "FK_UserCustomers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItem",
                columns: table => new
                {
                    InvoiceItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceRef = table.Column<int>(type: "int", nullable: false),
                    RowID = table.Column<int>(type: "int", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdditionFactor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PriceInBaseCurrency = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Duty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItem", x => x.InvoiceItemID);
                    table.ForeignKey(
                        name: "FK_InvoiceItem_Invoice_InvoiceRef",
                        column: x => x.InvoiceRef,
                        principalTable: "Invoice",
                        principalColumn: "InvoiceId");
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_ProductGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "ProductGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StateTXT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StateID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_States_StateID",
                        column: x => x.StateID,
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    _AttachType = table.Column<int>(type: "int", nullable: false),
                    _AttachTypeTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttachDateTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Filename = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Filetype = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fileExten = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    File = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    contractId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_Contracts_contractId",
                        column: x => x.contractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Product = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Count = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dimensions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Caset = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackgroundColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrintColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CountColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Weight = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Thickness = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DesignName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Delivered = table.Column<bool>(type: "bit", nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrderHeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_OrderHeads_OrderHeadId",
                        column: x => x.OrderHeadId,
                        principalTable: "OrderHeads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Factors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SumTotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SumTotalAmountTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PercentTax = table.Column<int>(type: "int", nullable: false),
                    TotalTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReciveAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateMiladi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCompletedTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SnFinancial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialNumber = table.Column<int>(type: "int", nullable: false),
                    CountNum = table.Column<int>(type: "int", nullable: false),
                    OfficialType = table.Column<byte>(type: "tinyint", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Customer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgentName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpireValidateDesc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ftype = table.Column<int>(type: "int", nullable: false),
                    ftypeTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Emza = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Signature = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    SignatureDes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PfactorSerialNumber = table.Column<int>(type: "int", nullable: false),
                    PfactorID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserTaminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserTaminTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Export = table.Column<bool>(type: "bit", nullable: true),
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
                    table.PrimaryKey("PK_Factors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Factors_PreFactors_PfactorID",
                        column: x => x.PfactorID,
                        principalTable: "PreFactors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreFactorDescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Order = table.Column<int>(type: "int", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dir = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrefactorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Official = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreFactorDescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreFactorDescriptions_PreFactors_PrefactorId",
                        column: x => x.PrefactorId,
                        principalTable: "PreFactors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreFactorItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Row = table.Column<int>(type: "int", nullable: false),
                    Product = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeightCount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    UnitTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitAmount = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(20,0)", precision: 20, scale: 0, nullable: false),
                    ProductGroup = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductGroupTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductCode = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductSmallName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Printable = table.Column<byte>(type: "tinyint", nullable: true),
                    PrefactorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_PreFactorItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreFactorItems_PreFactors_PrefactorId",
                        column: x => x.PrefactorId,
                        principalTable: "PreFactors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactorDescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Order = table.Column<int>(type: "int", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dir = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    factorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactorDescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactorDescriptions_Factors_factorId",
                        column: x => x.factorId,
                        principalTable: "Factors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactorItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Row = table.Column<int>(type: "int", nullable: false),
                    Product = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeightCount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    UnitTxt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    factorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductSmallName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PfItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_FactorItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactorItems_Factors_factorId",
                        column: x => x.factorId,
                        principalTable: "Factors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accountancies_CustomerId",
                table: "Accountancies",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_contractId",
                table: "Attachments",
                column: "contractId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_StateID",
                table: "Cities",
                column: "StateID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFile_CompanyId",
                table: "CompanyFile",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_CustomerId",
                table: "Contracts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_FactorDescriptions_factorId",
                table: "FactorDescriptions",
                column: "factorId");

            migrationBuilder.CreateIndex(
                name: "IX_FactorItems_factorId",
                table: "FactorItems",
                column: "factorId");

            migrationBuilder.CreateIndex(
                name: "IX_Factors_PfactorID",
                table: "Factors",
                column: "PfactorID");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItem_InvoiceRef",
                table: "InvoiceItem",
                column: "InvoiceRef");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHeads_Customer",
                table: "OrderHeads",
                column: "Customer");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderHeadId",
                table: "Orders",
                column: "OrderHeadId");

            migrationBuilder.CreateIndex(
                name: "IX_PreFactorDescriptions_PrefactorId",
                table: "PreFactorDescriptions",
                column: "PrefactorId");

            migrationBuilder.CreateIndex(
                name: "IX_PreFactorItems_PrefactorId",
                table: "PreFactorItems",
                column: "PrefactorId");

            migrationBuilder.CreateIndex(
                name: "IX_PreFactors_CustomerId",
                table: "PreFactors",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_GroupId",
                table: "Products",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RemindDays_CarizoId",
                table: "RemindDays",
                column: "CarizoId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCustomers_CustomerId",
                table: "UserCustomers",
                column: "CustomerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accountancies");

            migrationBuilder.DropTable(
                name: "ASettings");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "BaseDescriptions");

            migrationBuilder.DropTable(
                name: "BaseItems");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "CompanyFile");

            migrationBuilder.DropTable(
                name: "ExtraData");

            migrationBuilder.DropTable(
                name: "FactorDescriptions");

            migrationBuilder.DropTable(
                name: "FactorItems");

            migrationBuilder.DropTable(
                name: "InvoiceItem");

            migrationBuilder.DropTable(
                name: "logSystems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "PreFactorDescriptions");

            migrationBuilder.DropTable(
                name: "PreFactorItems");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Quotation");

            migrationBuilder.DropTable(
                name: "RemindDays");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "UserCustomers");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "States");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Factors");

            migrationBuilder.DropTable(
                name: "Invoice");

            migrationBuilder.DropTable(
                name: "OrderHeads");

            migrationBuilder.DropTable(
                name: "ProductGroups");

            migrationBuilder.DropTable(
                name: "Carizos");

            migrationBuilder.DropTable(
                name: "PreFactors");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
