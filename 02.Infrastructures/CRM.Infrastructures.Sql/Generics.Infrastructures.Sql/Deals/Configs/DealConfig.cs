using CRM.Core.Domain.Deals.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Deals.Configs;

public class DealConfig : IEntityTypeConfiguration<Deal> {
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.HasKey(f => f.Id);

//        builder.Property(f => f.SerialNumber).ValueGeneratedOnAdd();
    }
}