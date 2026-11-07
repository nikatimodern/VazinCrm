using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
namespace CRM.EndPoint.FrontEnd.Filters;
// تغییر نقش پس از صدور کوکی هم باید به پاسخ ممنوع منجر شود.
public sealed class ReleaseNoteAccessAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not UnauthorizedAccessException) return;
        context.Result = new ObjectResult(new { message = "اجازه دسترسی به این بخش را ندارید." }) { StatusCode = 403 };
        context.ExceptionHandled = true;
    }
}
