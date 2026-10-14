using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql {
    public class Sepidar2ContextDb : SepidarContextDbBase {
        public Sepidar2ContextDb(DbContextOptions<Sepidar2ContextDb> op) : base(op) { }
    }
}