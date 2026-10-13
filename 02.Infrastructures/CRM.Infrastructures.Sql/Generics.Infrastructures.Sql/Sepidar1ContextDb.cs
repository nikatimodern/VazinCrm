using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Sepidar.Accounts.Entites;
using CRM.Core.Domain.Sepidar.Banks.Entites;
using CRM.Core.Domain.Sepidar.Currencies.Entites;
using CRM.Core.Domain.Sepidar.DLs.Entites;
using CRM.Core.Domain.Sepidar.ExtraDataes;
using CRM.Core.Domain.Sepidar.FiscalYears.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Payments.Entites;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql {
    public class Sepidar1ContextDb : SepidarContextDbBase {
        public Sepidar1ContextDb(DbContextOptions<Sepidar1ContextDb> op) : base(op)
        {
            
        }
    }
}