using System.ComponentModel.DataAnnotations;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Login;

public class LoginOtpRequest {
    [Required(ErrorMessage = "وارد کردن شماره موبایل الزامی است")]
    [Phone(ErrorMessage = "شماره موبایل نامعتبر است")]
    public string Mobile { get; set; }
    public string UserId { get; set; }

}