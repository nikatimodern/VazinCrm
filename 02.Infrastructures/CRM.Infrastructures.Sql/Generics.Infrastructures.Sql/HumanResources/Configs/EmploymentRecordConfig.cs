using CRM.Core.Domain.HumanResources.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.HumanResources.Configs;

internal sealed class EmploymentRecordConfig : IEntityTypeConfiguration<EmploymentRecord>
{
    public void Configure(EntityTypeBuilder<EmploymentRecord> builder)
    {
        builder.ToTable("HrEmploymentRecords", table =>
            table.HasCheckConstraint("CK_HrEmploymentRecords_Dates", "[EndDate] IS NULL OR [EndDate] >= [StartDate]"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmploymentType).HasMaxLength(64).IsRequired();
        builder.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EmployeeId, x.StartDate }).IsUnique();
        builder.Property<byte[]>("RowVersion").IsRequired().IsRowVersion();
    }
}
