using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Composition;
using CRM.EndPoint.FrontEnd.Controllers;
using CRM.EndPoint.FrontEnd.IServices.Menu;
using CRM.EndPoint.FrontEnd.Models.Menu;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRM.Taxation.Tests;

internal static class TaxationHttpTestHost
{
    internal static async Task<WebApplication> StartAsync(DbContextOptions<CRMContextDb> options,
        DirectoryInfo keyRing, TaxationMockAuthority authority, CancellationToken ct)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = [],
            ApplicationName = typeof(TaxationController).Assembly.GetName().Name,
            EnvironmentName = "Development",
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Taxation:EnableLocalPreview"] = "true",
            ["Taxation:Sandbox:Enabled"] = "true",
            ["Taxation:Sandbox:BaseAddress"] = "https://sandboxrc.tax.gov.ir/requestsmanager/api/v2/"
        });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0));

        var services = builder.Services;
        services.AddControllersWithViews();
        services.AddDistributedMemoryCache();
        services.AddSession();
        services.AddDataProtection().SetApplicationName("Vazin.Taxation.HttpTests").PersistKeysToFileSystem(keyRing);
        services.AddAuthentication("TaxTest").AddScheme<AuthenticationSchemeOptions, TaxTestAuthentication>("TaxTest", _ => { });
        services.AddAuthorization();
        services.AddSingleton(options);
        services.AddScoped<CRMContextDb>();
        services.AddIdentityCore<AppUser>().AddUserStore<TestUsers>();
        services.AddSingleton<IMenuService, EmptyMenu>();
        services.AddScoped<ICqrsDispatcher, CqrsDispatcher>();
        services.AddTaxation();
        services.AddSingleton<ITaxSigningBindings>(authority);
        services.AddSingleton<ITaxAuthorityClient>(authority.Transport);
        services.AddSingleton<ITaxPaymentTransport>(authority.Transport);

        var app = builder.Build();
        app.UseDeveloperExceptionPage();
        app.UseRouting();
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapControllerRoute("default", "{controller}/{action=Index}/{id?}");
        try
        {
            await app.StartAsync(ct);
            return app;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    internal static HttpClient Connect(WebApplication app, string actor = "admin")
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(app.Urls.Single()),
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.Add("X-Tax-Test-Actor", actor);
        return client;
    }

    private sealed class EmptyMenu : IMenuService
    {
        public Task<List<MenuItem>> GetMenusAsync(ClaimsPrincipal user) => Task.FromResult<List<MenuItem>>([]);
    }

    private sealed class TestUsers : IUserStore<AppUser>
    {
        public Task<AppUser> FindByIdAsync(string id, CancellationToken ct) => Task.FromResult(new AppUser { Id = id, UserName = id });
        public Task<AppUser> FindByNameAsync(string name, CancellationToken ct) => throw new NotSupportedException();
        public Task<string> GetUserIdAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.Id);
        public Task<string> GetUserNameAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.UserName);
        public Task<string> GetNormalizedUserNameAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.NormalizedUserName);
        public Task SetUserNameAsync(AppUser user, string name, CancellationToken ct) => throw new NotSupportedException();
        public Task SetNormalizedUserNameAsync(AppUser user, string name, CancellationToken ct) => throw new NotSupportedException();
        public Task<IdentityResult> CreateAsync(AppUser user, CancellationToken ct) => throw new NotSupportedException();
        public Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken ct) => throw new NotSupportedException();
        public Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken ct) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class TaxTestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> configuration,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(configuration, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var actor = Request.Headers["X-Tax-Test-Actor"].ToString();
            if (string.IsNullOrEmpty(actor))
                return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, actor),
                new Claim(ClaimTypes.Role, actor == "admin" ? AppRoles.Admin : AppRoles.SalesExpert) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
