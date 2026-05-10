using CRM.Core.Domain.RequestAmounts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.RequestAmounts.Configs;

public class RequestAmountPaymentConfigs : IEntityTypeConfiguration<RequestAmountPayment> {
    public void Configure(EntityTypeBuilder<RequestAmountPayment> builder)
    {
        builder.HasKey(f => new { f.RequestAmountId, f.PaymentId });
        builder.Property(f => f.Amount).HasPrecision(20, 0);
    }
}