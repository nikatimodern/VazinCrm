using CRM.Core.Domain;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.CustomValidator;
using CRM.EndPoint.FrontEnd.Services.Factor;
using CRM.EndPoint.FrontEnd.Services.SaveChangeCustom;
using CRM.EndPoint.FrontEnd.Services.SetInterval;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.Framework;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Security.Claims;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.IServices.Ai;
using CRM.EndPoint.FrontEnd.IServices.DocumentRender;
using CRM.EndPoint.FrontEnd.IServices.Menu;
using CRM.EndPoint.FrontEnd.IServices.PushNotification;
using CRM.EndPoint.FrontEnd.IServices.Sms;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.Services.AI;
using CRM.EndPoint.FrontEnd.Models.Configuration;
using CRM.EndPoint.FrontEnd.Services.Asterisk;
using CRM.EndPoint.FrontEnd.Services.Carizo;
using CRM.EndPoint.FrontEnd.Services.Chat;
using CRM.EndPoint.FrontEnd.Services.DocumentRender;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.EndPoint.FrontEnd.Services.Menu;
using CRM.EndPoint.FrontEnd.Services.PushNotification;
using CRM.EndPoint.FrontEnd.Services.Sms;
using CRM.EndPoint.FrontEnd.Services.ViewRender;
using WebPush;

namespace CRM.EndPoint.FrontEnd {
    public class Startup {
        public IConfiguration Configuration { get; }


        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private static string _Decode(string EncodeText)
        {
            return EnCodeDeCode.Decrypt(EncodeText);
        }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(opt => {
                    opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                    opt.JsonSerializerOptions.WriteIndented = true;
                });


            //AddSingleton
            services.AddHostedService<TimedHostedService>();
            services.AddHostedService<AsteriskAmiListener>();

            services.AddAuthorizationBuilder()
                .AddPolicy("NotCustomerOnly", policy =>
                    policy.RequireAuthenticatedUser()
                        .RequireAssertion(context => !context.User.IsInRole("مشتری")));


            //All AddScoped
            services.AddDbContext<Sepidar1ContextDb>(op =>
                op.UseSqlServer(Configuration.GetConnectionString("Sepidar1Connection"))
            );

            services.AddDbContext<Sepidar2ContextDb>(op =>
                op.UseSqlServer(Configuration.GetConnectionString("Sepidar2Connection"))
            );

            services.AddDbContext<CRMContextDb>(op =>
                op.UseSqlServer(_Decode(Configuration.GetConnectionString("CRMConnection")))
            );


            services.AddDbContext<UserContextDb>(op =>
                op.UseSqlServer(_Decode(Configuration.GetConnectionString("UserConnection")), op => op.EnableRetryOnFailure())
            );

            /*services.AddDbContext<IsabelleContextDb>(options =>
                options.UseMySql(Configuration.GetConnectionString("IsabelleConnection"),
                    new MySqlServerVersion(new Version(10, 5, 0)) // نسخه‌ی MariaDB
                ));*/


            services.AddIdentity<AppUser, IdentityRole>
                (c => {
                        c.Password.RequireDigit = false;
                        c.Password.RequiredLength = 0;
                        c.Password.RequiredUniqueChars = 0;
                        c.Password.RequireLowercase = false;
                        c.Password.RequireNonAlphanumeric = false;
                        c.Password.RequireUppercase = false;

                        //c.User.RequireUniqueEmail = false;
                        //c.User.AllowedUserNameCharacters = "12345asdzxc";
                    }
                ).AddEntityFrameworkStores<UserContextDb>()
                .AddDefaultTokenProviders();


            services.Configure<SecurityStampValidatorOptions>(options => { options.ValidationInterval = TimeSpan.FromSeconds(5); });


            services.AddAuthentication(options => { options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; })
                .AddCookie(options => {
                    options.ExpireTimeSpan = TimeSpan.FromSeconds(5);
                    options.Cookie.MaxAge = options.ExpireTimeSpan; // optional
                    options.Cookie.Expiration = TimeSpan.FromSeconds(5);
                    options.SlidingExpiration = true;
                });

            services.AddTransient<IPasswordValidator<AppUser>, CustomPasswordValidator>();
            services.AddSession();
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddTransient<RequestServices>();
            services.AddControllersWithViews();
            services.AddScoped<DateTimeServices>();
            services.AddTransient<FileSaver>();
            services.AddScoped<Service>();
            services.AddTransient<SmsService>();
            services.AddHttpContextAccessor();
            services.AddScoped<SepidarService>();
            services.AddSignalR();
            services.AddTransient<ChatHub>();
            services.AddTransient<TaskServices>();
            services.AddTransient<CrmSaveCahngeAsync>();
            services.AddTransient<TimedHostedService>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IViewRenderService, ViewRenderService>();
            services.AddScoped<LogSystemService>();

            services.AddSingleton<IPushQueue, PushQueue>();
            services.AddHostedService<PushBackgroundService>();
            services.AddScoped<IPushService, PushService>();

            services.AddSingleton<IAiQueue, AiQueue>();
            services.AddHostedService<AiBackgroundService>();
            services.AddScoped<AiProcessAudio>();
            services.AddScoped<AiService>();
            services.AddScoped<ViraAi>();
            services.AddScoped<GapgptAi>();
            services.AddScoped<AiPrompts>();
            services.AddScoped<IssabelService>();
            services.AddHostedService<AiPendingWorker>();

            services.AddSingleton<ISmsQueue, SmsQueue>();
            services.AddHostedService<SmsBackgroundService>();

            services.AddHostedService<ChatReminderWorker>();
            services.AddScoped<ChatReminderService>();

            services.AddScoped<IDocumentRenderService, DocumentRenderService>();

            services.AddScoped<IMenuService, MenuService>();
            services.AddScoped<PreFactorService>();
            services.AddSingleton<ExcelExportService>();

            services.AddSingleton<WebPushClient>(sp => {
                var handler = new SocketsHttpHandler {
                    EnableMultipleHttp2Connections = false,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
                };

                var httpClient = new HttpClient(handler) {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                return new WebPushClient(httpClient);
            });

            services.AddHttpClient<ViraAi>(client => {
                client.BaseAddress = new Uri("https://partai.gw.isahab.ir/");
                client.DefaultRequestHeaders.Add("gateway-token",
                    "eyJhbGciOiJIUzI1NiJ9.eyJzeXN0ZW0iOiJzYWhhYiIsImNyZWF0ZVRpbWUiOiIxNDA1MDIyMzE0MjA0MTAwNiIsInVuaXF1ZUZpZWxkcyI6eyJ1c2VybmFtZSI6Ijg3YzM0ZDI2LTE0OWQtNGNlYS1hMTNhLTg3YWNhZWNmZjg4MiJ9LCJncm91cE5hbWUiOiJiZmNmZDI5YzJmOGJkMjA5M2NjMWIzM2ZkNzUxZTkyNiIsImRhdGEiOnsic2VydmljZUlEIjoiNGVkNzY5ZTYtNDgxNC00ZGNiLWEzZWQtZTU1ZWI5Y2FiZjhlIiwicmFuZG9tVGV4dCI6IklvQ1VSIn19.Hvu7g2cRpMnmV7gz6yn09m_zMKBab-d9d5StzTvtT7I");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            /*services.AddHttpClient("SummarizeApi", client =>
            {
                client.BaseAddress = new Uri("https://dadmatech.gw.isahab.ir/summarize/v1/summarize");
                client.DefaultRequestHeaders.Add("gateway-token", "eyJhbGciOiJIUzI1NiJ9.eyJzeXN0ZW0iOiJzYWhhYiIsImNyZWF0ZVRpbWUiOiIxNDA1MDEyNzE0MDEwNjYxMCIsInVuaXF1ZUZpZWxkcyI6eyJ1c2VybmFtZSI6Ijg3YzM0ZDI2LTE0OWQtNGNlYS1hMTNhLTg3YWNhZWNmZjg4MiJ9LCJncm91cE5hbWUiOiJiOTc2MWUzNTdmMDZiODFjZTFiNTQ0YjY1M2QyYTEzYiIsImRhdGEiOnsic2VydmljZUlEIjoiNTUyMWYxYjgtYmJiYS00ZGI0LWI0YTctYjA2OGI5NGQ1ZTQyIiwicmFuZG9tVGV4dCI6IkN3anp0In19.ez-ICJWTBcu3NJ_Og5ohFrjWj55hzUEDZO0xhnjxXWw");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });*/

            services.AddHttpClient<GapgptAi>(client => {
                client.BaseAddress = new Uri("https://api.gapgpt.app/v1/");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", "sk-DqOst76fwb3UtC05ZW1mraSTwfsJtz6B8xPpXuWfwCjEm2Uw");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            services.AddHttpClient<AiProcessAudio>(client => {
                client.BaseAddress = new Uri("http://192.168.1.149:8000/");

                client.Timeout = TimeSpan.FromMinutes(5);

                client.DefaultRequestHeaders.Add("GAPGPT_API_KEY", "sk-DqOst76fwb3UtC05ZW1mraSTwfsJtz6B8xPpXuWfwCjEm2Uw");
                client.DefaultRequestHeaders.Add("VIRA_API_KEY",
                    "eyJhbGciOiJIUzI1NiJ9.eyJzeXN0ZW0iOiJzYWhhYiIsImNyZWF0ZVRpbWUiOiIxNDA1MDIyMzE0MjA0MTAwNiIsInVuaXF1ZUZpZWxkcyI6eyJ1c2VybmFtZSI6Ijg3YzM0ZDI2LTE0OWQtNGNlYS1hMTNhLTg3YWNhZWNmZjg4MiJ9LCJncm91cE5hbWUiOiJiZmNmZDI5YzJmOGJkMjA5M2NjMWIzM2ZkNzUxZTkyNiIsImRhdGEiOnsic2VydmljZUlEIjoiNGVkNzY5ZTYtNDgxNC00ZGNiLWEzZWQtZTU1ZWI5Y2FiZjhlIiwicmFuZG9tVGV4dCI6IklvQ1VSIn19.Hvu7g2cRpMnmV7gz6yn09m_zMKBab-d9d5StzTvtT7I");
                client.DefaultRequestHeaders.Add("User", "admin");
                client.DefaultRequestHeaders.Add("Pass", "admin@54321");
            });

            services.AddHttpClient<SmsService>(client => {
                var smsConfig = Configuration?.GetSection("smsConfig").Get<appSettingSms>();

                client.BaseAddress = new Uri(smsConfig?._Url);
                client.DefaultRequestHeaders.Add("Api-Key", smsConfig?._apiKey);
            });

            services
                .AddControllersWithViews()
                .AddRazorRuntimeCompilation();

            services.ConfigureApplicationCookie(options => {
                options.Events = new CookieAuthenticationEvents {
                    OnRedirectToAccessDenied = context => {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    },
                    OnRedirectToLogin = context => {
                        if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            var currentPage = context.Request.Headers["X-Current-Page"].ToString();

                            var loginUrl = "/Account/Login";

                            if (!string.IsNullOrWhiteSpace(currentPage))
                            {
                                loginUrl += $"?ReturnUrl={Uri.EscapeDataString(currentPage)}";
                            }

                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.Headers["X-Auth-Expired"] = "true";
                            context.Response.Headers["X-Login-Url"] = loginUrl;

                            return Task.CompletedTask;
                        }

                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    }
                };
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            /*var contentRoot = env.ContentRootPath;
            var licenseFile = System.IO.Path.Combine(contentRoot, "wwwroot\\Reports", "license.key");
            Stimulsoft.Base.StiLicense.LoadFromFile(licenseFile);*/

            //app.UseSerilogRequestLogging();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            } else
            {
                app.UseExceptionHandler("/Error/500"); // فقط برای exceptionهای واقعی (500+)
                app.UseStatusCodePagesWithReExecute("/Error/{0}"); // برای status code ها مثل 404 / 401 / 403

                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "account-otp",
                    pattern: "Account/OtpLogin/{type?}",
                    defaults: new { controller = "Account", action = "OtpLogin" }
                );

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Account}/{action=OtpLogin}/{id?}"
                );
                endpoints.MapHub<ChatHub>("/ChatHub");
                endpoints.MapHub<TaskHub>("/TaskHub");
                endpoints.MapHub<CallHub>("/CallHub");
            });
        }
    }
}