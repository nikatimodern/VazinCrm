using CRM.Core.Domain.Sepidar;
using CRM.Core.Domain.Sepidar.Accounts.Entites;
using CRM.Core.Domain.Sepidar.Banks.Entites;
using CRM.Core.Domain.Sepidar.Currencies.Entites;
using CRM.Core.Domain.Sepidar.DLs.Entites;
using CRM.Core.Domain.Sepidar.ExtraDataes;
using CRM.Core.Domain.Sepidar.FiscalYears.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Items;
using CRM.Core.Domain.Sepidar.Parties;
using CRM.Core.Domain.Sepidar.Payments.Entites;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql;

public abstract class SepidarContextDbBase : DbContext {
    protected SepidarContextDbBase(DbContextOptions options) : base(options) { }


    public DbSet<ExtraData> ExtraData { get; set; }
    public DbSet<Quotation> vwQuotation { get; set; }
    public DbSet<Invoice> vwInvoice { get; set; }
    public DbSet<InvoiceItem> vwInvoiceItem { get; set; }
    public DbSet<PaymentHeader> PaymentHeaders { get; set; }
    public DbSet<FiscalYear> FiscalYears { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<DL> DLs { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<PaymentDraft> PaymentDrafts { get; set; }
    public DbSet<SepidarBankAccount> SepidarBankAccounts { get; set; }
    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<VoucherItem> VoucherItems { get; set; }
    public DbSet<SepidarBank> SepidarBanks { get; set; }
    public DbSet<SepidarBankBranch> SepidarBankBranches { get; set; }

    public DbSet<Quotation> Quotations { get; set; }
    public DbSet<QuotationItem> QuotationItems { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<Party> Parties { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);

        modelBuilder.Entity<Quotation>().ToView("vwQuotation", "SLS");
        modelBuilder.Entity<Invoice>().ToView("vwInvoice", "SLS").Ignore(f => f.DateTXT);
        modelBuilder.Entity<InvoiceItem>().ToView("vwInvoiceItem", "SLS");
        modelBuilder.Entity<ExtraData>().ToTable("ExtraData", "FMK");


        modelBuilder.Entity<PaymentDraft>().ToTable("PaymentDraft", "RPA");
        modelBuilder.Entity<PaymentHeader>().ToTable("PaymentHeader", "RPA");
        modelBuilder.Entity<FiscalYear>().ToTable("FiscalYear", "FMK");
        modelBuilder.Entity<Currency>().ToTable("Currency", "GNR");
        modelBuilder.Entity<DL>().ToTable("DL", "ACC");
        modelBuilder.Entity<Account>().ToTable("Account", "ACC");
        modelBuilder.Entity<Voucher>().ToTable("Voucher", "ACC");
        modelBuilder.Entity<VoucherItem>().ToTable("VoucherItem", "ACC");


        modelBuilder.Entity<SepidarBank>().ToTable("Bank", "RPA");
        modelBuilder.Entity<SepidarBankBranch>().ToTable("BankBranch", "RPA");
        modelBuilder.Entity<SepidarBankAccount>().ToTable("BankAccount", "RPA");

        modelBuilder.Entity<Quotation>().ToTable("Quotation", "SLS");
        modelBuilder.Entity<QuotationItem>().ToTable("QuotationItem", "SLS");
        modelBuilder.Entity<Item>().ToTable("Item", "INV");
        modelBuilder.Entity<Party>().ToTable("Party", "GNR");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }
}