using CRM.Core.Domain.Asterisk.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql;

public class IsabelleContextDb : DbContext
{
    public IsabelleContextDb(DbContextOptions<IsabelleContextDb> options) :base(options) {

    }
   public DbSet<Cdr> Cdr { get; set; }
}