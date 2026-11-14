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
        public DbSet<OtpMobileState> OtpMobileStates { get; set; }
        public DbSet<PasswordLockoutState> PasswordLockoutStates { get; set; }
        public DbSet<AccessEntity> AccessEntities { get; set; }
        public DbSet<AccessPermission> AccessPermissions { get; set; }
        public DbSet<AccessRolePermission> AccessRolePermissions { get; set; }
        public DbSet<AccessUserPermission> AccessUserPermissions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);


            // The old method was declared without "override" and its configurations were
            // never part of the effective Identity model. Keep the existing AppUser/Position
            // relationship and BaseAdmin mapping by convention, as in the existing migrations.
            // Re-applying the old WithOne/NoAction settings here would change the Identity schema.
            // Only the access catalog configurations are newly registered in this context.
            modelBuilder.ApplyConfiguration(new AccessEntityConfiguration());
            modelBuilder.ApplyConfiguration(new AccessPermissionConfiguration());
            modelBuilder.ApplyConfiguration(new AccessRolePermissionConfiguration());
            modelBuilder.ApplyConfiguration(new AccessUserPermissionConfiguration());


        }
    }
}