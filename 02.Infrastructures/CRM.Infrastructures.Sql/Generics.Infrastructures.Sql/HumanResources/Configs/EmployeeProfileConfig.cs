using CRM.Core.Domain.HumanResources.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.HumanResources.Configs;

internal sealed class EmployeeProfileConfig : IEntityTypeConfiguration<EmployeeProfile>
{
    public void Configure(EntityTypeBuilder<EmployeeProfile> builder)
    {
        builder.ToTable("HrEmployeeProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PersonnelCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AppUserId).HasMaxLength(450);
        builder.HasIndex(x => x.PersonnelCode).IsUnique();
        builder.HasIndex(x => x.AppUserId).IsUnique().HasFilter("[AppUserId] IS NOT NULL");
        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
