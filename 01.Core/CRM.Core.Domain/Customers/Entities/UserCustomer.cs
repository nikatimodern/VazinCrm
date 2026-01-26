using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CRM.Core.Domain.Customers.Entities
{
    public class UserCustomer
    {
        
        public Guid Id { get; set; } 
        public Guid UserId { get; set; }
        public Guid CustomerId { get; set; }
 


    }
}
