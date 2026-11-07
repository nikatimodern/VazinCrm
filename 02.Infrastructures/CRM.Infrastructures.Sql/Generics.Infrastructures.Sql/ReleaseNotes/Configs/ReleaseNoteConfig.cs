using CRM.Core.Domain.ReleaseNotes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CRM.Infrastructures.Sql.ReleaseNotes.Configs;
public sealed class ReleaseNoteConfig : IEntityTypeConfiguration<ReleaseNote>
{
    public void Configure(EntityTypeBuilder<ReleaseNote> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Version).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.Version).IsUnique().HasDatabaseName("IX_ReleaseNotes_Version");
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.Status, x.PublishDate });
        b.HasMany(x => x.Items).WithOne(x => x.ReleaseNote).HasForeignKey(x => x.ReleaseNoteId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Roles).WithOne(x => x.ReleaseNote).HasForeignKey(x => x.ReleaseNoteId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.UserViews).WithOne(x => x.ReleaseNote).HasForeignKey(x => x.ReleaseNoteId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class ReleaseNoteItemConfig : IEntityTypeConfiguration<ReleaseNoteItem>
{
    public void Configure(EntityTypeBuilder<ReleaseNoteItem> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.ImageUrl).HasMaxLength(2048);
        b.Property(x => x.LearnMoreUrl).HasMaxLength(2048);
        b.HasIndex(x => new { x.ReleaseNoteId, x.SortOrder });
    }
}
public sealed class ReleaseNoteRoleConfig : IEntityTypeConfiguration<ReleaseNoteRole>
{
    public void Configure(EntityTypeBuilder<ReleaseNoteRole> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.RoleName).HasMaxLength(256).IsRequired();
        b.HasIndex(x => new { x.ReleaseNoteId, x.RoleName }).IsUnique();
    }
}
public sealed class UserReleaseNoteConfig : IEntityTypeConfiguration<UserReleaseNote>
{
    public void Configure(EntityTypeBuilder<UserReleaseNote> b)
    {
        b.ToTable("UserReleaseNotes");
        b.HasKey(x => x.Id);
        // کاربران در پایگاه جدا هستند؛ رابطه بین دو پایگاه تعریف نمی‌شود.
        b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        b.HasIndex(x => new { x.UserId, x.ReleaseNoteId }).IsUnique();
    }
}
