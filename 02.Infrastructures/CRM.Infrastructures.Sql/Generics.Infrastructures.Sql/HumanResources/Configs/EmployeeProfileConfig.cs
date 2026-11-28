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
        builder.Property(x => x.FatherName).HasMaxLength(100);
        builder.Property(x => x.NationalCode).HasMaxLength(10);
        builder.Property(x => x.BirthCertificateNumber).HasMaxLength(32);
        builder.Property(x => x.BirthDate).HasColumnType("date");
        builder.Property(x => x.Mobile).HasMaxLength(24);
        builder.Property(x => x.Telephone).HasMaxLength(24);
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Education).HasMaxLength(100);
        builder.Property(x => x.EmergencyContactName).HasMaxLength(150);
        builder.Property(x => x.EmergencyContactPhone).HasMaxLength(24);
        builder.HasIndex(x => x.NationalCode).IsUnique().HasFilter("[NationalCode] IS NOT NULL");
        builder.HasIndex(x => x.PersonnelCode).IsUnique();
        builder.HasIndex(x => x.AppUserId).IsUnique().HasFilter("[AppUserId] IS NOT NULL");
        builder.Property<byte[]>("RowVersion").IsRequired().IsRowVersion();
    }
}
