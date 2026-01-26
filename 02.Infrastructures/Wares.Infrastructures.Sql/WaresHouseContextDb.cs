using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wares.Core.Domain.WaresHouses.Entities;

namespace Wares.Infrastructures.Sql
{
    public class WaresHouseContextDb : DbContext
    {

        public WaresHouseContextDb(DbContextOptions<WaresHouseContextDb> op) : base(op)
        {


        }
        public DbSet<WaresHouse>WaresHouses { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        }


    }
}
