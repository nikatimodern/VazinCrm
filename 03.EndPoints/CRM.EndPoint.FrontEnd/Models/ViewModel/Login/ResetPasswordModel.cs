using System.ComponentModel.DataAnnotations;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Login;

public class ResetPasswordModel {
    [Required(ErrorMessage = "رمز عبور الزامی است")]
    [MinLength(3, ErrorMessage = "رمز عبور باید حداقل 3 کاراکتر باشد")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "تکرار رمز عبور الزامی است")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "رمز عبور و تکرار آن یکسان نیست")]
    public string ConfirmPassword { get; set; }
}