using CRM.Core.Domain.AccessCatalog.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.Identity;

namespace CRM.Infrastructures.Sql.AccessCatalog;

public sealed class AccessEntityConfiguration : IEntityTypeConfiguration<AccessEntity>
{
    public void Configure(EntityTypeBuilder<AccessEntity> entity)
    {
        entity.ToTable("AccessEntities");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Key).IsRequired().HasMaxLength(64);
        entity.Property(x => x.Title).IsRequired().HasMaxLength(150);
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.Property(x => x.DbContextName).IsRequired().HasMaxLength(128);
        entity.Property(x => x.ModelFullName).IsRequired().HasMaxLength(400);
        entity.HasIndex(x => x.Key).IsUnique();
        entity.HasIndex(x => new { x.DbContextName, x.ModelFullName }).IsUnique();
        entity.HasMany(x => x.Permissions).WithOne(x => x.AccessEntity)
            .HasForeignKey(x => x.AccessEntityId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AccessPermissionConfiguration : IEntityTypeConfiguration<AccessPermission>
{
    public void Configure(EntityTypeBuilder<AccessPermission> permission)
    {
        permission.ToTable("AccessPermissions");
        permission.HasKey(x => x.Id);
        permission.Property(x => x.Key).IsRequired().HasMaxLength(64);
        permission.Property(x => x.Title).IsRequired().HasMaxLength(150);
        permission.Property(x => x.Description).HasMaxLength(1000);
        permission.HasIndex(x => new { x.AccessEntityId, x.Key }).IsUnique();
    }
}

public sealed class AccessRolePermissionConfiguration : IEntityTypeConfiguration<AccessRolePermission>
{
    public void Configure(EntityTypeBuilder<AccessRolePermission> entity)
    {
        entity.ToTable("AccessRolePermissions");
        entity.HasKey(x => new { x.RoleId, x.AccessPermissionId });
        entity.Property(x => x.RoleId).IsRequired().HasMaxLength(450);
        entity.HasIndex(x => x.AccessPermissionId);
        entity.HasOne(x => x.AccessPermission).WithMany()
            .HasForeignKey(x => x.AccessPermissionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<IdentityRole>().WithMany()
            .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}
