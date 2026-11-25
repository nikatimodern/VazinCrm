using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Cartables.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Generics.Infrastructures.Sql {
    public class UserContextDb : IdentityDbContext<AppUser> {
        public UserContextDb(DbContextOptions<UserContextDb> op) : base(op) { }

        public DbSet<BaseAdmin> BaseAdmins { get; set; }
        public DbSet<OtpMobileState> OtpMobileStates { get; set; }
        public DbSet<PasswordLockoutState> PasswordLockoutStates { get; set; }
        public DbSet<MenuFavorite> MenuFavorites { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            // The old non-override OnModelCreating was never executed by EF Core.
            // Configure only the new menu feature here to preserve the effective
            // Identity and application model mappings used before this feature.
            modelBuilder.Entity<MenuFavorite>(entity => {
                entity.HasKey(f => new { f.UserId, f.MenuId });
                entity.Property(f => f.UserId).HasMaxLength(450).IsRequired();
                entity.Property(f => f.MenuId).HasMaxLength(100).IsRequired();
            });
        }
    }
}