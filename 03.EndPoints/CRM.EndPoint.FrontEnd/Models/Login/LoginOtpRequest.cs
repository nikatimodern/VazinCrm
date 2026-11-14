using System.ComponentModel.DataAnnotations;

namespace CRM.EndPoint.FrontEnd.Models.Login;

public class LoginOtpRequest {
    [Required(ErrorMessage = "وارد کردن شماره موبایل الزامی است")]
    public string Mobile { get; set; }
    public string UserId { get; set; }

}