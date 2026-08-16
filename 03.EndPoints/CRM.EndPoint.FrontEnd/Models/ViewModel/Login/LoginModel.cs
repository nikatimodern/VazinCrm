using System.ComponentModel.DataAnnotations;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Login
{
    public class LoginModel
    {
        [Required(ErrorMessage = "نام کاربری الزامی است")]
        [UIHint("UserName")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [UIHint("password")]
        public string Password { get; set; }

        public string returnUrl { get; set; }
    }
}
