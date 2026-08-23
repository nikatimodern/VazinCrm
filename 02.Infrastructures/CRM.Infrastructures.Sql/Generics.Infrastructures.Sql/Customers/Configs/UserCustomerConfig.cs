using CRM.Core.Domain.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Customers.Configs
{
    public class UserCustomerConfig : IEntityTypeConfiguration<UserCustomer>
    {
        public void Configure(EntityTypeBuilder<UserCustomer> builder)
        {
            builder.HasKey(f => f.Id);
        }
    }
}
