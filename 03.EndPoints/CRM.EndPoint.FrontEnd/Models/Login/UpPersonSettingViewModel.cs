using Microsoft.AspNetCore.Http;

namespace CRM.EndPoint.FrontEnd.Models.Login
{
    public class UpPersonSettingViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public IFormFile PersonImage { get; set; }
        public int imageStatus { get; set; }

    }
}
