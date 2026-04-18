using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Services;

public class ErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await FileLogger.LogErrorAsync(ex);
            throw; // برای جلوگیری از مخفی شدن خطا
        }
    }
}