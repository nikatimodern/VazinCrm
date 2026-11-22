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


        protected void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<AppUser>().HasOne(u => u.Position)
                .WithOne()
                .HasForeignKey<AppUser>(f => f.PositionId)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<MenuFavorite>().HasKey(f => new { f.UserId, f.MenuId });
            modelBuilder.Entity<MenuFavorite>().Property(f => f.UserId).HasMaxLength(450);
            modelBuilder.Entity<MenuFavorite>().Property(f => f.MenuId).HasMaxLength(100);

            modelBuilder.Entity<BaseAdmin>().HasKey(f => f.Id);
            modelBuilder.Entity<BaseAdmin>().HasQueryFilter(f => f.IsDeleted == false);


        }
    }
}