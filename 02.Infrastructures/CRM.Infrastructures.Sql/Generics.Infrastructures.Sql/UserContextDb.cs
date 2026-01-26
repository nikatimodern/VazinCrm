using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Cartables.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Generics.Infrastructures.Sql {
    public class UserContextDb : IdentityDbContext<AppUser> {
        public UserContextDb(DbContextOptions<UserContextDb> op) : base(op) { }

        public DbSet<BaseAdmin> BaseAdmins { get; set; }


        protected void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<AppUser>().HasOne(u => u.Position)
                .WithOne()
                .HasForeignKey<AppUser>(f => f.PositionId)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<BaseAdmin>().HasKey(f => f.Id);
            modelBuilder.Entity<BaseAdmin>().HasQueryFilter(f => f.IsDeleted == false);


        }
    }
}