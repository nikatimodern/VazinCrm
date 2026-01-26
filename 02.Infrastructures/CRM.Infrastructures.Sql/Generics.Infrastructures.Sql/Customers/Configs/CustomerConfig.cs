using CRM.Core.Domain.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.Customers.Configs {
    public class CustomerConfig : IEntityTypeConfiguration<Customer> {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.HasKey(f => f.Id);

            builder.Property(f => f.CustomerCode).ValueGeneratedOnAdd();

            builder.HasMany(f => f.orderHeads)
                .WithOne()
                .HasForeignKey(f => f.Customer)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.PreFactors)
                .WithOne(f => f.CustomerEntity)
                .HasForeignKey(f => f.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.Accountancies)
                .WithOne()
                .HasForeignKey(f => f.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.Contacts)
                .WithOne()
                .HasForeignKey(f => f.EntityId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.Deals)
                .WithOne(f => f.Customer)
                .HasForeignKey(f => f.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}