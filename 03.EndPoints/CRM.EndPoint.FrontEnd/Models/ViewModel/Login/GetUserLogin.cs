using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Login
{
    public class GetUserLogin
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string RollName { get; set; }
        public List<RoleInfo> RollNames { get; set; }
        public string PersonImageUrl { get; set; }
        public byte[] PersonImage { get; set; }
        public string PhoneNumber { get; set; }

    }

    public class RoleInfo
    {
        public string Name { get; set; }   // مقدار انگلیسی
        public string FaName { get; set; } // مقدار فارسی
    }

}
