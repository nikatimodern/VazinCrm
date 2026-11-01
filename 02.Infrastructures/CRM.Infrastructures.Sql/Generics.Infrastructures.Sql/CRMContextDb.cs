using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.BaseItems.Entities;
using CRM.Core.Domain.CallRecordings.Entities;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Cartables.Entities;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Chats.Entities;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Cities.Entities;
using CRM.Core.Domain.Company.Entities;
using CRM.Core.Domain.Contacts.Entities;
using CRM.Core.Domain.Contracts.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Emails.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Products.Entities;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.Sms.Entities;
using CRM.Core.Domain.States.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.SystemWarnings.Entities;
using CRM.Core.Domain.Transfers.Entities;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.Core.Domain.WebPushSubscriptions.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql {
    public class CRMContextDb : DbContext {
        public CRMContextDb(DbContextOptions<CRMContextDb> op) : base(op) { }

        public DbSet<Accountancy> Accountancies { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<BaseItem> BaseItems { get; set; }
        public DbSet<OrderHead> OrderHeads { get; set; }
        public DbSet<Order> Orders { get; set; }

        public DbSet<PreFactor> PreFactors { get; set; }
        public DbSet<PreFactorItem> PreFactorItems { get; set; }
        public DbSet<PreFactorDescription> PreFactorDescriptions { get; set; }

        public DbSet<Factor> Factors { get; set; }
        public DbSet<FactorItem> FactorItems { get; set; }
        public DbSet<FactorDescription> FactorDescriptions { get; set; }

        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyFile> CompanyFile { get; set; }
        public DbSet<BaseDescription> BaseDescriptions { get; set; }
        public DbSet<UserCustomer> UserCustomers { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductGroup> ProductGroups { get; set; }
        public DbSet<ProductAmount> ProductAmounts { get; set; }
        public DbSet<ASetting> ASettings { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Carizo> Carizos { get; set; }
        public DbSet<TaskUser> TaskUsers { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<LogSystem> logSystems { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<Supply> Supplies { get; set; }
        public DbSet<SystemWarning> SystemWarnings { get; set; }
        public DbSet<Deal> Deals { get; set; }

        public DbSet<Agent> Agents { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Cartable> Cartables { get; set; }
        public DbSet<Email> Emails { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<MainChat> MainChats { get; set; }
        public DbSet<WebPushSubscription> WebPushSubscriptions { get; set; }
        public DbSet<SupplyFactor> SupplyFactors { get; set; }
        public DbSet<SupplyFactorOrderHead> SupplyFactorOrderHeads { get; set; }
        public DbSet<RequestAmount> RequestAmounts { get; set; }
        public DbSet<RequestAmountSupplyFactor> RequestAmountSupplyFactors { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentSupplyFactor> PaymentSupplyFactors { get; set; }
        public DbSet<Cashier> Cashiers { get; set; }
        public DbSet<Cash> Cashes { get; set; }
        public DbSet<BaseCash> BaseCashes { get; set; }
        public DbSet<BaseCenterCash> BaseCenterCashes { get; set; }
        public DbSet<RequestAmountOrderHead> RequestAmountOrderHeads { get; set; }
        public DbSet<RequestAmountPayment> RequestAmountPayments { get; set; }
        public DbSet<CallRecording> CallRecordings { get; set; }
        public DbSet<CallParticipant> CallParticipants { get; set; }
        public DbSet<Checkbook> Checkbooks { get; set; }
        public DbSet<Check> Checks { get; set; }
        public DbSet<Transfer> Transfers { get; set; }
        public DbSet<CashGroup> CashGroups { get; set; }
        public DbSet<CashOrderHead> CashOrderHeads { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseProduct> WarehouseProducts { get; set; }
        public DbSet<WarehouseTransaction> WarehouseTransactions { get; set; }
        public DbSet<WarehouseUser> WarehouseUsers { get; set; }
        public DbSet<WarehouseVoucher> WarehouseVouchers { get; set; }
        public DbSet<WarehouseProductVoucher> WarehouseProductVouchers { get; set; }
        public DbSet<SupplyFactorItemAllocation> SupplyFactorItemAllocations { get; set; }
        public DbSet<SupplyFactorItem> SupplyFactorItems { get; set; }
        public DbSet<ReceivedSms> ReceivedSmses { get; set; }
        public DbSet<SentSms> SentSmses { get; set; }
        public DbSet<ExternalMapping> ExternalMappings { get; set; }
        public DbSet<ExternalSoftware> ExternalSoftwares { get; set; }
        public DbSet<CashierExternalSoftware> CashierExternalSoftwares { get; set; }
        public DbSet<CashExternalSoftware> CashExternalSoftwares { get; set; }
        public DbSet<ExternalSoftwareSetting> ExternalSoftwareSettings { get; set; }
        public DbSet<AccountancyTransfer> AccountancyTransfers { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //GetEnumerableOfType();
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
            //modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContractConfig).Assembly);


            modelBuilder.Entity<Customer>().HasQueryFilter(f => f.IsDeleted == false);
            //modelBuilder.Entity<State>().HasQueryFilter(f => f.IsDeleted == false);
            //modelBuilder.Entity<City>().HasQueryFilter(f => f.IsDeleted == false);
            //modelBuilder.Entity<BaseItem>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<OrderHead>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Order>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<PreFactor>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<PreFactorItem>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Factor>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<FactorItem>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<FactorDescription>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Company>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Product>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Attachment>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<PreFactorDescription>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Accountancy>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Contract>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Carizo>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<TaskUser>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Report>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<ProductAmount>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<BankAccount>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Supply>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<SystemWarning>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Deal>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Contract>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Agent>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Cartable>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Email>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Chat>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<MainChat>().HasQueryFilter(f => f.IsDeleted == false);

            modelBuilder.Entity<SupplyFactor>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<SupplyFactorOrderHead>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<RequestAmountSupplyFactor>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<RequestAmount>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Payment>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<PaymentSupplyFactor>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Cashier>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Cash>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<RequestAmountOrderHead>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<RequestAmountPayment>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<CallRecording>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Checkbook>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Check>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Transfer>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<BaseCash>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<BaseCenterCash>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<CashGroup>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<CashOrderHead>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<Warehouse>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<WarehouseTransaction>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<WarehouseProduct>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<WarehouseVoucher>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<WarehouseProductVoucher>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<SupplyFactorItem>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<ReceivedSms>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<SentSms>().HasQueryFilter(f => f.IsDeleted == false);
            modelBuilder.Entity<AccountancyTransfer>().HasQueryFilter(f => f.IsDeleted == false);


            modelBuilder.Entity<Order>().Ignore(f => f.Row);
            modelBuilder.Entity<PreFactor>().Ignore(f => f.Emza);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}