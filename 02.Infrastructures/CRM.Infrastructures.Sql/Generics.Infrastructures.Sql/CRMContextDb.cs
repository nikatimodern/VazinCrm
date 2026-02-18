using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.BaseItems.Entities;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Cities.Entities;
using CRM.Core.Domain.Company.Entities;
using CRM.Core.Domain.Contacts.Entities;
using CRM.Core.Domain.Contracts.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Products.Entities;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.States.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.SystemWarnings.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Cartables.Entities;
using CRM.Core.Domain.Chats.Entities;
using CRM.Core.Domain.Emails.Entities;
using CRM.Core.Domain.WebPushSubscriptions.Entities;

namespace Generics.Infrastructures.Sql
{
    public class CRMContextDb : DbContext
    {

       

        public CRMContextDb(DbContextOptions<CRMContextDb> op) : base(op)
        {

           
        }

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



            modelBuilder.Entity<Order>().Ignore(f => f.Row);
            modelBuilder.Entity<PreFactor>().Ignore(f => f.Emza);



        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}
