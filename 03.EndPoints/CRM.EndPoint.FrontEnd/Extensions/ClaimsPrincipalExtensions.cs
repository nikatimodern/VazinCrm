using System.Security.Claims;
using CRM.EndPoint.FrontEnd.Services;

namespace CRM.EndPoint.FrontEnd.Extensions;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user) {
        public bool IsAdmin() => user.IsInRole(AppRoles.Admin);

        public bool IsSales() => user.IsInRole(AppRoles.SalesExpert) || user.IsInRole(AppRoles.SalesAdmin);

        public bool IsSalesExpert() => user.IsInRole(AppRoles.SalesExpert);

        public bool IsSalesAdmin() => user.IsInRole(AppRoles.SalesAdmin);

        public bool IsTamin() => user.IsInRole(AppRoles.TaminExpert) || user.IsInRole(AppRoles.TaminAdmin);

        public bool IsTaminExpert() => user.IsInRole(AppRoles.TaminExpert);

        public bool IsTaminAdmin() => user.IsInRole(AppRoles.TaminAdmin);

        public bool IsFinancial() => user.IsInRole(AppRoles.FinancialExpert) || user.IsInRole(AppRoles.FinancialAdmin);

        public bool IsFinancialExpert() => user.IsInRole(AppRoles.FinancialExpert);

        public bool IsFinancialAdmin() => user.IsInRole(AppRoles.FinancialAdmin);

        public bool IsManagingDirector() => user.IsInRole(AppRoles.ManagingDirector);

        public bool IsExecutiveDirector() => user.IsInRole(AppRoles.ExecutiveDirector);

        public bool IsTanKhah() => user.IsInRole(AppRoles.TanKhah);

        public bool IsCustomer() => user.IsInRole(AppRoles.Customer);

        public string GetUserId() => user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}