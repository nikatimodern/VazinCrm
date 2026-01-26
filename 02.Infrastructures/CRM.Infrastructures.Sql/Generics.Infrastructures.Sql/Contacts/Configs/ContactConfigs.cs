using CRM.Core.Domain.Contacts.Entities;
using CRM.Core.Domain.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Contacts.Configs
{
    public class ContactConfigs : IEntityTypeConfiguration<Contact>
    {
        public void Configure(EntityTypeBuilder<Contact> builder)
        {
            builder.HasKey(f => f.Id);
        }
    }
}
