using CRM.Core.Domain.Warehouses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Warehouses.Configs;

public class WarehouseConfig : IEntityTypeConfiguration<Warehouse> {
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasKey(x => x.Id);
    }
}