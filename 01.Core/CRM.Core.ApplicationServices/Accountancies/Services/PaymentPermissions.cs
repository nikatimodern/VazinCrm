using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.ApplicationServices.Accountancies.Dtos;

namespace CRM.Core.ApplicationServices.Accountancies.Services;
public static class PaymentPermissions
{
    public static void Apply(PaymentPartialData model, IList<string> roles, AppUser user)
    {
        model.IsAdmin = roles.Contains("مدیر ارشد");
        model.IsSalesAdmin = roles.Contains("مدیر فروش");
        model.IsManagingDirector = roles.Contains("مدیر عامل") || user.NormalizedUserName == "A.NIROMAND";
        model.IsTamin = roles.Contains("مدیر تامین") || roles.Contains("کارشناس تامین");
        model.IsFinancial = roles.Contains("کارشناس مالی") || roles.Contains("مدیر مالی");
        model.IsCustomer = roles.Contains("مشتری");
    }
}
