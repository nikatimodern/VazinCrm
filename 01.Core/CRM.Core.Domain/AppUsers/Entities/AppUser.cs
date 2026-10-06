using CRM.Core.Domain.Cartables.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CRM.Framework;

namespace CRM.Core.Domain.AppUsers.Entities {
    public class AppUser : IdentityUser {
        //public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));

        public byte[] PersonImage { get; set; }
        public string PersonImageUrl { get; set; }
        [NotMapped] public IList<string> RolsUsers { get; set; }
        public bool OrganAdmin { get; set; } = false;


        public BaseAdmin Position { get; set; } //BaseAdmin
        public Guid? PositionId { get; set; } //BaseAdmin
        public string PositionTxt { get; set; } //BaseAdminTxt
        public string InternalTels { get; set; }

        public Guid? PersonId { get; set; }
        public bool IsEmployee { get; set; }
        
        
        public DateTime? CreateDate { get; set; } = DateTime.Now;
        public string CreateDateTxt { get; set; } = BaseServices.GetDateTimePersian();

    }
}