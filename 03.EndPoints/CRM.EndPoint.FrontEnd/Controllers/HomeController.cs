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
        private readonly AudioToTextService audioToTextService;

        public HomeController(IWebHostEnvironment appEnvironment, TimedHostedService hostService, IConfiguration conf,
            UserManager<AppUser> userManager, ILogger<HomeController> logger, CRMContextDb db, DateTimeServices dts,
            Sepidar1ContextDb sdb, Service genService, UserContextDb udb, Sepidar1ContextDb S1db, AudioToTextService audioToTextService)
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
            this.audioToTextService = audioToTextService;
            this.genService = genService;
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public async Task<IActionResult> Index()
        {
            await using var trans = await db.Database.BeginTransactionAsync();
            try
            {
                ViewBag.SubTitle = "سامانه مدیریت مشتری";

                /*var text =
                    "آره الو، سلام وقت خیر سلام علیکم وقت شما زنده باشید. ممنون قربان. شما جان. نایلون شفاف کیلویی چنده؟ نایلون شفاف الان کیلویی چهارصد و پنجاه هزار تومن بعد حالا ما برای برند مون می خوایم بذاریم کلیشه اش اینا هزینه اش چقدره؟ اون موقع باید طرح و ابعاد و همه رو بفرستیم بهتون قیمت بشه. قیمت ها هم لحظه ای و دقیقه ایه بعد ارسال به قوم هم دارید؟ بله، می ریم بابه برون بیاره دیگه درسته بعد اینکه حالا رنگش سفیده ها ولی نایلون سفید شفافه مشکل نداره. همون چهارصد و پنجاه. کلا آره خامشون حالا طرح و اینا رو باید بفرستید که اون موقع قیمت نهاییش رو بتونه. الان که نمی شه قیمت اینجوری. حالا کلیشه ش اینا حدودا چقدر اصلا چاپش دو رنگه. اون رشد چهارصد و الان شصت هفتاد تومن چاپش می شه. کلی شش حشه، هفت تومن، شیش تومن کلیشه تقریبا صد و هفت پنجاه و شصت تومن چهارصد و پنجاه تا چهارصد و شصت هزار تومن چهارصد و شصت دست شما درد نکنه. خواهش می کنم قربان شما نیکس چند سر نیکس اونم تو همون یه ذره بالا یه ذره نایلکس هم چهارصد و شصت تقریبا دست شما درد نکنه. شما آقای من حسین زادهم آقای حسین زاده بعد من خواستم تماس بگیرم داخلی چند رو بگیرم. صد و هفت هم من صد و هفت تا ساعت چند تشریف دارید. شما دو چهار صد تحویل چند روزه است. بی چند تا سی روز کاری سی روزه به درسته، دست شما درد نکنه، شما خداحافظ. ";
*/
/*

                var text =
                    "جانم سلام سلام علیکم وقت بخیر خسته نباشید. من شما ممنون داداش شما ناکس نی تولید می کنید. ناکس نونی هم تولید می کنید ولی قیمت اولش مشخص نیست. اجازه بدید یه ساعت دیگه قیمت دربیاد. بهتون اعلام کنید موجود دارید نداری. م. باید تولید کنیم. آماده نداریم. ببین مال شما پوست پیازی یا نه، یه خورده کلفت تره. چربی داره. می آد یه یه کیلو چند ضخامتش هر طرف می تونه چهار ضخامت هر لایه سی میونه منظورت بدونه. داداش عرض به به شما یعنی تعداد مثلا نازک تر می خوام دیگه تعداد بالا می خوام تعداد بالا بده دیگه منظورم همینه دیگه. حداقل تولیدمون روی همین سینه، چقدره سیمیون، سیمیون سیم چجوریه؟ من سر در نمی آرم. داداش من بدونم بهتره دیگه؟ به خاطر مفتم یه لحظه ابعاد رمضونتون رو بگید. چند کیلو طول داداش شما پنجاه و پنج، هفتاد و پنج دارید باید تولید کنه. آماده باشه. خب، پنجاه و پنج، هشت شما پنجاه هشتاد دارید. منظورم ببین. ببین من تعداد بالا می خوام. اگه باشه، من الان نزدیک یک تن بار می خوام مشکل نیست، ولی تعداد بده دیگه، نه که کلفت باشه که مشتری به مشتری پس بده. می دونی هرچقدر نازک تر باشه، مشتری پسند می شه. هر چقدر هشت، پنجاه و پنج، هشتاد سی و هشت ه در می آد چهل تا در چهل تا آره، نه داداش اون به من نگفت قربتم. من معذرت خوام از دستت درد نکنه. هر خدا خدا شم خداست، ";

                var res = await audioToTextService.SummarizeText(text);
*/

                /*
           string ftpHost = "192.168.1.150"; // IP سرور ایزابل
                string ftpUser = "root"; // نام کاربری FTP در ایزابل
                string ftpPass = "123"; // رمز عبور FTP
                string remoteFilePath = "/var/spool/asterisk/monitor/2026/04/08/rg-600-9123906718-20260408-160736-1775648256.116234.wav";
                string targetFilePath = @"wwwroot\recordings"; // مسیر ذخیره فایل در سرور ASP.NET


                DownloadRecording(ftpHost, ftpUser, ftpPass, remoteFilePath, targetFilePath);


                var localFilePath = Path.Combine(targetFilePath, Path.GetFileName(remoteFilePath));
                if (!System.IO.File.Exists(localFilePath))
                    return NotFound("File not found");

                var result = await audioToTextService.ConvertAsync(localFilePath);

*/
                //await audioToTextService.ProcessCallRecording("", "", "");
                await trans.CommitAsync();
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