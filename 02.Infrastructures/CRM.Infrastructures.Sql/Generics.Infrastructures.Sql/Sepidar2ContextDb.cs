using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql
{
    public class Sepidar2ContextDb:DbContext
    {
        public Sepidar2ContextDb(DbContextOptions<Sepidar2ContextDb> op) : base(op)
        {


        }
        public DbSet<Quotation> vwQuotation { get; set; }
        public DbSet<Invoice> vwInvoice { get; set; }
        public DbSet<InvoiceItem> vwInvoiceItem { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);

            modelBuilder.Entity<Quotation>().ToView("vwQuotation", "SLS");
            modelBuilder.Entity<Invoice>().ToView("vwInvoice", "SLS");
            modelBuilder.Entity<InvoiceItem>().ToView("vwInvoiceItem", "SLS");


        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

        }

    }
}
