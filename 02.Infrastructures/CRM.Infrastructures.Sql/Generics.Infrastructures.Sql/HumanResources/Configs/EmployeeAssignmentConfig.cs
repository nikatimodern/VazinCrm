using CRM.Core.Domain.HumanResources.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.HumanResources.Configs;

internal sealed class EmployeeAssignmentConfig : IEntityTypeConfiguration<EmployeeAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeAssignment> builder)
    {
        builder.ToTable("HrEmployeeAssignments", table =>
        {
            table.HasCheckConstraint("CK_HrEmployeeAssignments_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
            table.HasCheckConstraint("CK_HrEmployeeAssignments_Manager", "[ManagerEmployeeId] IS NULL OR [ManagerEmployeeId] <> [EmployeeId]");
        });
        builder.HasKey(x => x.Id);
        builder.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x => x.ManagerEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<JobPosition>().WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom });
        builder.Property<byte[]>("RowVersion").IsRequired().IsRowVersion();
    }
}
