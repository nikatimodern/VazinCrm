using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Login
{
    public class LoginResponse
    {
        public int Status { get; set; }
        public string ErrorMessage { get; set; }
        public string UrlRedirect { get; set; }

    }
}
