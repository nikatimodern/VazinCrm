using CRM.Core.Domain.HumanResources.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.HumanResources.Configs;

internal sealed class EmploymentContractConfig : IEntityTypeConfiguration<EmploymentContract>
{
    public void Configure(EntityTypeBuilder<EmploymentContract> builder)
    {
        builder.ToTable("HrEmploymentContracts", table =>
        {
            table.HasCheckConstraint("CK_HrContracts_Term", "([Term] = 1 AND [EndDate] IS NOT NULL AND [EndDate] >= [StartDate]) OR ([Term] = 2 AND [EndDate] IS NULL)");
            table.HasCheckConstraint("CK_HrContracts_Predecessor", "[PreviousContractId] IS NULL OR [PreviousContractId] <> [Id]");
            table.HasCheckConstraint("CK_HrContracts_Status", "[Status] IN (1, 2, 3, 4)");
            table.HasCheckConstraint("CK_HrContracts_Termination", "([Status] = 4 AND [TerminatedOn] IS NOT NULL AND [TerminatedOn] >= [StartDate] AND ([EndDate] IS NULL OR [TerminatedOn] <= [EndDate])) OR ([Status] <> 4 AND [TerminatedOn] IS NULL)");
            table.HasCheckConstraint("CK_HrContracts_Approval", "([Status] IN (2, 4) AND [ApprovedByUserId] IS NOT NULL AND [ApprovedAtUtc] IS NOT NULL) OR ([Status] IN (1, 3) AND [ApprovedByUserId] IS NULL AND [ApprovedAtUtc] IS NULL)");
            table.HasCheckConstraint("CK_HrContracts_Resolution", "([Status] IN (3, 4) AND [ResolvedByUserId] IS NOT NULL AND [ResolvedAtUtc] IS NOT NULL AND [ResolutionReason] IS NOT NULL) OR ([Status] IN (1, 2) AND [ResolvedByUserId] IS NULL AND [ResolvedAtUtc] IS NULL AND [ResolutionReason] IS NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ContractNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ApprovedByUserId).HasMaxLength(450);
        builder.Property(x => x.ResolvedByUserId).HasMaxLength(450);
        builder.Property(x => x.ResolutionReason).HasMaxLength(1000);
        builder.HasIndex(x => x.ContractNumber).IsUnique();
        builder.HasIndex(x => new { x.EmployeeId, x.StartDate });
        builder.HasIndex(x => new { x.Status, x.EndDate, x.Id });
        builder.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        // Composite key prevents linking a renewal to another employee's contract.
        builder.HasAlternateKey(x => new { x.EmployeeId, x.Id });
        builder.HasOne<EmploymentContract>().WithMany()
            .HasForeignKey(x => new { x.EmployeeId, x.PreviousContractId })
            .HasPrincipalKey(x => new { x.EmployeeId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property<byte[]>("RowVersion").IsRequired().IsRowVersion();
    }
}
