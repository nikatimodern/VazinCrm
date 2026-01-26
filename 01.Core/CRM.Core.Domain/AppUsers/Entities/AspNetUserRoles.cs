using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.AppUsers.Entities
{
    public class AspNetUserRoles : IdentityUserRole<string>
    {
        public int UserType { get; set; }
        public string UserTypeTxt { get; set; }


    }
}
