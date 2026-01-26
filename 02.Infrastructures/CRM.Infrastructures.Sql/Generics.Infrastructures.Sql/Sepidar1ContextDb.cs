using CRM.Core.Domain.Sepidar.ExtraDataes;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql
{
    public class Sepidar1ContextDb:DbContext
    {
        public Sepidar1ContextDb(DbContextOptions<Sepidar1ContextDb> op) : base(op)
        {


        }

        public DbSet<ExtraData> ExtraData { get; set; }
        public DbSet<Quotation> vwQuotation { get; set; }
        public DbSet<Invoice> vwInvoice { get; set; }
        public DbSet<InvoiceItem> vwInvoiceItem { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);

            modelBuilder.Entity<Quotation>().ToView("vwQuotation", "SLS");
            modelBuilder.Entity<Invoice>().ToView("vwInvoice", "SLS").Ignore(f=>f.DateTXT);
            modelBuilder.Entity<InvoiceItem>().ToView("vwInvoiceItem", "SLS");
            modelBuilder.Entity<ExtraData>().ToTable("ExtraData", "FMK");


        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);



        }




    }
}
