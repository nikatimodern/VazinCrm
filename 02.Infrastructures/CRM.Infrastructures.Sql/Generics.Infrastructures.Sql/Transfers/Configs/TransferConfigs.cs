using CRM.Core.Domain.Transfers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Transfers.Configs;

public class TransferConfigs : IEntityTypeConfiguration<Transfer> {
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TransferNumber).ValueGeneratedOnAdd();
        builder.Property(t => t.Amount).HasPrecision(20, 0);
    }
}