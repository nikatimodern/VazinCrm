using CRM.Core.Domain.AppUsers.Entities;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Admin
{
    public class AdminViewModel
    {
        public List<AppUser> Users { get; set; }
        public List<IdentityRole> Rols { get; set; }
    }
}
