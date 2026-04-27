using CRM.Core.Domain;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.CustomValidator;
using CRM.EndPoint.FrontEnd.Services.Factor;
using CRM.EndPoint.FrontEnd.Services.SaveChangeCustom;
using CRM.EndPoint.FrontEnd.Services.SetInterval;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.EndPoint.FrontEnd.Services.TaskServices;
using CRM.Framework;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Security.Claims;
using CRM.EndPoint.FrontEnd.Services.PushServices;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using CRM.EndPoint.FrontEnd.Services.AI;
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
                op.UseSqlServer(Configuration.GetConnectionString("Sepidar1Connection")
                ));

            services.AddDbContext<Sepidar2ContextDb>(op =>
                op.UseSqlServer(Configuration.GetConnectionString("Sepidar2Connection")
                ));

            services.AddDbContext<CRMContextDb>(op =>
                op.UseSqlServer(_Decode(Configuration.GetConnectionString("CRMConnection"))
                ));


            services.AddDbContext<UserContextDb>(op =>
                op.UseSqlServer(_Decode(Configuration.GetConnectionString("UserConnection")),
                    op => op.EnableRetryOnFailure()
                ));

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
            services.AddTransient<SmsServices>();
            services.AddHttpContextAccessor();
            services.AddScoped<SepidarService>();
            services.AddSignalR();
            services.AddTransient<ChatHub>();
            services.AddTransient<TaskServices>();
            services.AddTransient<CrmSaveCahngeAsync>();
            services.AddTransient<TimedHostedService>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IVeiwRenderService, VeiwRenderService>();
            services.AddScoped<LogSystemService>();

            services.AddSingleton<IPushQueue, PushQueue>();
            services.AddHostedService<PushBackgroundService>();
            services.AddScoped<IPushService, PushService>();

            services.AddSingleton<IAiQueue, AiQueue>();
            services.AddHostedService<AiBackgroundService>();
            services.AddScoped<AudioToTextService>();
            services.AddScoped<AiService>();
            services.AddScoped<ViraAi>();
            services.AddScoped<GapgptAi>();

            services.AddSingleton<WebPushClient>(sp =>
            {
                var handler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = false,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
                };

                var httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                return new WebPushClient(httpClient);
            });


            services.AddHttpClient("AvanegarApi", client =>
            {
                client.BaseAddress = new Uri("https://partai.gw.isahab.ir/avanegar/v2/avanegar/request");
                client.DefaultRequestHeaders.Add("gateway-token", "eyJhbGciOiJIUzI1NiJ9.eyJzeXN0ZW0iOiJzYWhhYiIsImNyZWF0ZVRpbWUiOiIxNDA1MDExNzEwMDkwNzA0MiIsInVuaXF1ZUZpZWxkcyI6eyJ1c2VybmFtZSI6Ijg3YzM0ZDI2LTE0OWQtNGNlYS1hMTNhLTg3YWNhZWNmZjg4MiJ9LCJncm91cE5hbWUiOiJiZmNmZDI5YzJmOGJkMjA5M2NjMWIzM2ZkNzUxZTkyNiIsImRhdGEiOnsic2VydmljZUlEIjoiNGVkNzY5ZTYtNDgxNC00ZGNiLWEzZWQtZTU1ZWI5Y2FiZjhlIiwicmFuZG9tVGV4dCI6IlhiMFpsIn19.NttwGRfzzklqvXglCgf7tmvPYRnq0t8PtV4xjmbQQ2Q");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            /*services.AddHttpClient("SummarizeApi", client =>
            {
                client.BaseAddress = new Uri("https://dadmatech.gw.isahab.ir/summarize/v1/summarize");
                client.DefaultRequestHeaders.Add("gateway-token", "eyJhbGciOiJIUzI1NiJ9.eyJzeXN0ZW0iOiJzYWhhYiIsImNyZWF0ZVRpbWUiOiIxNDA1MDEyNzE0MDEwNjYxMCIsInVuaXF1ZUZpZWxkcyI6eyJ1c2VybmFtZSI6Ijg3YzM0ZDI2LTE0OWQtNGNlYS1hMTNhLTg3YWNhZWNmZjg4MiJ9LCJncm91cE5hbWUiOiJiOTc2MWUzNTdmMDZiODFjZTFiNTQ0YjY1M2QyYTEzYiIsImRhdGEiOnsic2VydmljZUlEIjoiNTUyMWYxYjgtYmJiYS00ZGI0LWI0YTctYjA2OGI5NGQ1ZTQyIiwicmFuZG9tVGV4dCI6IkN3anp0In19.ez-ICJWTBcu3NJ_Og5ohFrjWj55hzUEDZO0xhnjxXWw");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });*/

            services.AddHttpClient("GapgptApi", client =>
            {
                client.BaseAddress = new Uri("https://api.gapgpt.app/v1/");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "sk-AP1Dqd3THpHbfx3c1O6Aif1Q1CExaBuWTWaFFaJ6E88lQsCe");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            services
                .AddControllersWithViews()
                .AddRazorRuntimeCompilation();

            services.ConfigureApplicationCookie(options => {
                options.AccessDeniedPath = "/AccessDenied-Skip"; // مسیر جعلی که ما خودمون مدیریت می‌کنیم

                options.Events = new CookieAuthenticationEvents {
                    OnRedirectToAccessDenied = async context => {
                        var user = context.HttpContext.User;
                        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                        if (!string.IsNullOrEmpty(userId))
                        {
                            var requestPath = context.Request.Path.Value?.ToLower();
                            Console.WriteLine($"[AccessDenied] Path: {requestPath} | Time: {DateTime.Now}");

                            if (user.IsInRole("مشتری") || user.IsInRole("تامین کننده"))
                            {
                                //var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                                //var loginUser = await userManager.GetUserAsync(user);

                                context.Response.StatusCode = 302;

                                context.Response.Redirect("/Account/Home");
                                //context.Response.Redirect($"/Customer/CustomerProfile/{loginUser.PersonId}");
                                return;
                            }

                            var hubContext = context.HttpContext.RequestServices.GetRequiredService<IHubContext<TaskHub>>();

                            // ارسال نوتیف
                            await hubContext.Clients.User(userId).SendAsync("ShowAccessDeniedMessage", "شما به این بخش دسترسی ندارید!");


                            context.Response.StatusCode = 204;
                            return;
                        }

                        // اگر احراز هویت نشده بود، همچنان بفرست به لاگین
                        context.Response.Redirect(context.RedirectUri);
                    }
                };
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var contentRoot = env.ContentRootPath;
            var licenseFile = System.IO.Path.Combine(contentRoot, "wwwroot\\Reports", "license.key");
            //Stimulsoft.Base.StiLicense.LoadFromFile(licenseFile);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            } else
            {
                app.UseExceptionHandler("/Error/500"); // فقط برای exceptionهای واقعی (500+)
                app.UseStatusCodePagesWithReExecute("/Error/{0}"); // برای status code ها مثل 404 / 401 / 403

                // app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            app.UseMiddleware<ErrorLoggingMiddleware>();

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