using System.Collections.Generic;
using CRM.Core.Domain.AppUsers.Entities;
using Microsoft.AspNetCore.Identity;

namespace CRM.EndPoint.FrontEnd.Models.Admin
{
    public class AdminViewModel
    {
        public List<AppUser> Users { get; set; }
        public List<IdentityRole> Roles { get; set; }
    }
}
