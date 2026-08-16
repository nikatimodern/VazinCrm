using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Login
{
    public class CreateModel
    {

        [Required(ErrorMessage = "نام کاربری اجباریست")]
        [DisplayName("نام کاربری")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "تلفن همراه اجباریست")]
        [DisplayName("تلفن همراه")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "رمز عبور اجباریست")]
        [DisplayName("رمز عبور")]
        public string Password { get; set; }

        [Required(ErrorMessage = "نام اجباریست")]
        [DisplayName("نام")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "نام خانوادگی اجباریست")]
        [DisplayName("نام خانوادگی")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "پست الکترونیکی اجباریست")]
        [DisplayName("پست الکترونیکی")]
        public string Email { get; set; }



    }
}
