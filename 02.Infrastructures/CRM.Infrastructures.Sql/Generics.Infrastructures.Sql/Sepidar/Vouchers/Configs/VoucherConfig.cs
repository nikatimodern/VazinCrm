using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Vouchers.Configs;

public class VoucherConfig : IEntityTypeConfiguration<Voucher> {
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.HasMany(f => f.VoucherItems)
            .WithOne()
            .HasForeignKey(f => f.VoucherRef)
            .OnDelete(DeleteBehavior.Cascade);
    }
}