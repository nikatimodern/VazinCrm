using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Infrastructures.Sql.AccessCatalog;
using CRM.Core.Domain.Cartables.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Generics.Infrastructures.Sql {
    public class UserContextDb : IdentityDbContext<AppUser> {
        public UserContextDb(DbContextOptions<UserContextDb> op) : base(op) { }

        public DbSet<BaseAdmin> BaseAdmins { get; set; }
        public DbSet<AccessEntity> AccessEntities { get; set; }
        public DbSet<AccessPermission> AccessPermissions { get; set; }
        public DbSet<AccessRolePermission> AccessRolePermissions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<AppUser>().HasOne(u => u.Position)
                .WithOne()
                .HasForeignKey<AppUser>(f => f.PositionId)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<BaseAdmin>().HasKey(f => f.Id);
            modelBuilder.Entity<BaseAdmin>().HasQueryFilter(f => f.IsDeleted == false);

            // Explicit registration; CRMContextDb excludes these configurations from its scan.
            modelBuilder.ApplyConfiguration(new AccessEntityConfiguration());
            modelBuilder.ApplyConfiguration(new AccessPermissionConfiguration());
            modelBuilder.ApplyConfiguration(new AccessRolePermissionConfiguration());


        }
    }
}