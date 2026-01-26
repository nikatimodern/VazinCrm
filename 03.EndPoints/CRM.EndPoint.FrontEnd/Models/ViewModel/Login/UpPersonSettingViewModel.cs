using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Login
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
