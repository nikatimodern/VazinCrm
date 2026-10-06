using CRM.Core.Domain.Sepidar.Accounts.Entites;
using CRM.Core.Domain.Sepidar.Currencies.Entites;
using CRM.Core.Domain.Sepidar.DLs.Entites;
using CRM.Core.Domain.Sepidar.ExtraDataes;
using CRM.Core.Domain.Sepidar.FiscalYears.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Payments.Entites;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql {
    public class Sepidar1ContextDb : DbContext {
        public Sepidar1ContextDb(DbContextOptions<Sepidar1ContextDb> op) : base(op) { }

        public DbSet<ExtraData> ExtraData { get; set; }
        public DbSet<Quotation> vwQuotation { get; set; }
        public DbSet<Invoice> vwInvoice { get; set; }
        public DbSet<InvoiceItem> vwInvoiceItem { get; set; }
        public DbSet<PaymentHeader> PaymentHeaders { get; set; }
        public DbSet<FiscalYear> FiscalYears { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<DL> DLs { get; set; }
        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);

            modelBuilder.Entity<Quotation>().ToView("vwQuotation", "SLS");
            modelBuilder.Entity<Invoice>().ToView("vwInvoice", "SLS").Ignore(f => f.DateTXT);
            modelBuilder.Entity<InvoiceItem>().ToView("vwInvoiceItem", "SLS");
            modelBuilder.Entity<ExtraData>().ToTable("ExtraData", "FMK");
            modelBuilder.Entity<PaymentHeader>().ToTable("PaymentHeader", "RPA");

            modelBuilder.Entity<FiscalYear>().ToTable("FiscalYear", "FMK");
            modelBuilder.Entity<Currency>().ToTable("Currency", "GNR");
            modelBuilder.Entity<DL>().ToTable("DL", "ACC");
            modelBuilder.Entity<Account>().ToTable("Account", "ACC");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}