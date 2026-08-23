using CRM.Core.Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Orders.Configs {
    public class OrderHeadConfig : IEntityTypeConfiguration<OrderHead> {
        public void Configure(EntityTypeBuilder<OrderHead> builder)
        {
            builder.HasKey(f => f.Id);

            builder.Property(u => u.OrderNumber).ValueGeneratedOnAdd();
            //builder.HasKey(f => f.OrderNumber);
            builder.HasMany(f => f.Orders)
                .WithOne(f => f.OrderHead)
                .HasForeignKey(f => f.OrderHeadId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.HasMany(f => f.SupplyFactorOrderHeads)
                .WithOne()
                .HasForeignKey(f => f.OrderHeadId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.HasMany(f => f.RequestAmountOrderHeads)
                .WithOne(f => f.OrderHead)
                .HasForeignKey(f => f.OrderHeadId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(x => x.Deal)
                .WithMany(x => x.OrderHeads)
                .HasForeignKey(x => x.DealId)
                .OnDelete(DeleteBehavior.NoAction);
            
            builder.HasMany(f => f.CashOrderHeads)
                .WithOne(f => f.OrderHead)
                .HasForeignKey(f => f.OrderHeadId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}