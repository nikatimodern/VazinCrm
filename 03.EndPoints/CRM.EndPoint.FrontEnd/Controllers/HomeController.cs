using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.SetInterval;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.RequestAmounts.Enums;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;
using Microsoft.AspNetCore.Http;
using WebPush;
using System;
using System.IO;
using System.Net;
using Renci.SshNet;
using System.Net.Http;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Services.AI;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize]
    public class HomeController : Controller {
        private readonly IWebHostEnvironment appEnvironment;
        private readonly TimedHostedService hostService;
        private readonly IConfiguration conf;
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<HomeController> _logger;
        private readonly CRMContextDb db;
        private readonly DateTimeServices dts;
        private readonly Sepidar1ContextDb sdb;
        private readonly Service genService;
        private readonly UserContextDb udb;
        private readonly Sepidar1ContextDb S1db;

        public HomeController(IWebHostEnvironment appEnvironment, TimedHostedService hostService, IConfiguration conf,
            UserManager<AppUser> userManager, ILogger<HomeController> logger, CRMContextDb db, DateTimeServices dts,
            Sepidar1ContextDb sdb, Service genService, UserContextDb udb, Sepidar1ContextDb S1db)
        {
            this.appEnvironment = appEnvironment;
            this.hostService = hostService;
            this.conf = conf;
            this.userManager = userManager;
            _logger = logger;
            this.db = db;
            this.dts = dts;
            this.sdb = sdb;
            this.udb = udb;
            this.S1db = S1db;
            this.genService = genService;
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public async Task<IActionResult> Index()
        {
            await using var trans = await db.Database.BeginTransactionAsync();
            try
            {
                await trans.CommitAsync();

                var data = await db.RequestAmounts.IgnoreQueryFilters().ToListAsync();
                foreach (var r in data)
                {
                    if (r.RequestStatus == (RequestStatusEnum)1)
                    {
                        r.RequestStatus = RequestStatusEnum.در_انتظار_تایید_مدیرعامل;
                    } else if (r.RequestStatus == (RequestStatusEnum)2)
                    {
                        if (r.Vosoled)
                        {
                            r.RequestStatus = RequestStatusEnum.پرداخت_شده;
                        } else
                        {
                            r.RequestStatus = RequestStatusEnum.در_انتظار_پرداخت;
                        }
                    }

                    r.RequestStatusTxt = r.RequestStatus.ToString().Replace("_", " ");


                    if (r.Prepayment)
                    {
                        r.RequestAmountType = RequestAmountTypeEnum.پیش_پرداخت;
                        r.RequestAmountTypeTxt = nameof(RequestAmountTypeEnum.پیش_پرداخت).Replace("_", " ");
                    } else
                    {
                        r.RequestAmountType = RequestAmountTypeEnum.پرداخت_فاکتور;
                        r.RequestAmountTypeTxt = nameof(RequestAmountTypeEnum.پرداخت_فاکتور).Replace("_", " ");
                    }
                }

                //await db.SaveChangesAsync();
                ViewBag.SubTitle = "سامانه مدیریت مشتری";
                return View();
            }
            catch (Exception e)
            {
                await trans.RollbackAsync();
                return BadRequest(e.Message);
            }
        }


        public static void DownloadRecording(string host, string username, string password, string remoteFilePath, string localDirectory)
        {
            try
            {
                // ساخت مسیر کامل فایل محلی با حفظ نام فایل اصلی
                string localFileName = Path.GetFileName(remoteFilePath);
                string localFilePath = Path.Combine(localDirectory, localFileName);

                // اتصال به سرور SFTP
                using (var client = new SftpClient(host, username, password))
                {
                    Console.WriteLine($"Connecting to SFTP server {host}...");
                    client.Connect();
                    Console.WriteLine("Connected.");

                    // اطمینان از وجود دایرکتوری مقصد
                    if (!Directory.Exists(localDirectory))
                    {
                        Directory.CreateDirectory(localDirectory);
                        Console.WriteLine($"Created local directory: {localDirectory}");
                    }

                    // دانلود فایل
                    Console.WriteLine($"Downloading file: {remoteFilePath} to {localFilePath}");
                    using (var fileStream = System.IO.File.OpenWrite(localFilePath))
                    {
                        client.DownloadFile(remoteFilePath, fileStream);
                    }

                    Console.WriteLine("File downloaded successfully!");

                    // قطع اتصال
                    client.Disconnect();
                    Console.WriteLine("Disconnected.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                // در اینجا می‌توانید جزئیات بیشتری از خطا را نمایش دهید یا لاگ کنید
                // Console.WriteLine(ex.ToString());
            }
        }


        public async Task<string> DownloadRecordingWithAuthAsync(
            string recordingFileName,
            string isabelBaseUrl,
            string username,
            string password,
            string localSaveDirectory)
        {
            string fullRecordingUrl =
                "http://192.168.1.150/var/spool/asterisk/monitor/2026/04/08/rg-600-9123906718-20260408-160736-1775648256.116234.wav";
//            string fullRecordingUrl = new Uri(new Uri(isabelBaseUrl), recordingFileName).ToString();


            if (!Directory.Exists(localSaveDirectory))
            {
                Directory.CreateDirectory(localSaveDirectory);
            }

            string localFilePath = Path.Combine(localSaveDirectory, Path.GetFileName(recordingFileName));

            try
            {
                using (var httpClient = new HttpClient())
                {
                    string credentials = $"{username}:{password}";
                    string encodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);

                    var response = await httpClient.GetAsync(fullRecordingUrl, HttpCompletionOption.ResponseHeadersRead);

                    if (response.IsSuccessStatusCode)
                    {
                        await using (var contentStream = await response.Content.ReadAsStreamAsync())
                        await using (var fileStream = System.IO.File.Create(localFilePath))
                        {
                            await contentStream.CopyToAsync(fileStream);
                        }

                        Console.WriteLine($"File downloaded successfully to: {localFilePath}");
                        return localFilePath;
                    } else
                    {
                        Console.WriteLine($"Error downloading file: HTTP Status {response.StatusCode}");
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error content: {errorContent}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception downloading file: {ex.Message}");
                return null;
            }
        }


        public IActionResult sigR()
        {
            return View();
        }

        public IActionResult notif()
        {
            return View();
        }

        public IActionResult GetDate(int type)
        {
            switch (type)
            {
                case 1: //getCurrentWeek
                    return Ok(dts.GetCurrentWeek());
                case 2: //getCurrentMonth
                    return Ok(dts.GetCurrentMounth());
                case 3: //getCurrentYear
                    return Ok(dts.GetCurrentYear());
                case 4: //getToday_Date
                    return Ok(dts.GetDatePersian());
                case 5: //getToday_DateTime
                    return Ok(dts.GetDateTimePersianWithDayOfWeek());
                case 6: //getTowmarrowDate
                    return Ok(dts.GetTowmarrow());
            }

            return Ok();
        }


        public IActionResult GetTodayDateTime()
        {
            var dt = dts.GetDateTimePersianWithDayOfWeek();
            return Ok(dt);
        }

        public IActionResult GetTodayDate()
        {
            return Ok(dts.GetDatePersian());
        }

        public string GetTomorrowDate()
        {
            return dts.GetTowmarrow();
        }


        [HttpGet]
        public async Task<IActionResult> WarningsSepidarCrmFactors()
        {
            await hostService.CompareSepidarCrmFactors();
            var data = await db.SystemWarnings.ToListAsync();
            return Ok(data);
        }

        public async Task<IActionResult> GetAllSystemWarnings()
        {
            var warnings = await db.SystemWarnings.ToListAsync();
            return Ok(warnings);
        }


        [HttpGet]
        public async Task<IActionResult> DeleteSystemWarning(Guid id)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var item = await db.SystemWarnings.Where(f => f.Id == id).FirstOrDefaultAsync();
                if (item == null)
                    return BadRequest(new { message = "مقداری برای حذف یافت نشد" });


                item.IsDeleted = true;
                item.ModifyDate = DateTime.Now;
                item.ModifyDateTxt = dts.GetDateTimePersian();
                item.UserModifyID = user.Id;
                item.UserModify = user.FullName;
                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}