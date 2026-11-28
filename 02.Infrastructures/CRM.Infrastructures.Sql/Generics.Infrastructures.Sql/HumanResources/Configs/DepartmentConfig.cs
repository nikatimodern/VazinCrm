using CRM.Core.Domain.HumanResources.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.HumanResources.Configs;

internal sealed class DepartmentConfig : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("HrDepartments", table =>
            table.HasCheckConstraint("CK_HrDepartments_Parent", "[ParentId] IS NULL OR [ParentId] <> [Id]"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<byte[]>("RowVersion").IsRequired().IsRowVersion();
    }
}
