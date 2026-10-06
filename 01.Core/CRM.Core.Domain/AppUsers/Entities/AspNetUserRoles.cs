using Microsoft.AspNetCore.Identity;

namespace CRM.Core.Domain.AppUsers.Entities
{
    public class AspNetUserRoles : IdentityUserRole<string>
    {
        public int UserType { get; set; }
        public string UserTypeTxt { get; set; }


    }
}
