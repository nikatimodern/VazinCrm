namespace CRM.EndPoint.FrontEnd.Models.Login
{
    public class LoginResponse
    {
        public int Status { get; set; }
        public string ErrorMessage { get; set; }
        public string UrlRedirect { get; set; }

    }
}
