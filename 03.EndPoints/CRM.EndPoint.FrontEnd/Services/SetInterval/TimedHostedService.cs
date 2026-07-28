using CRM.Core.Domain;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.SystemWarnings.Entities;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.EndPoint.FrontEnd.ISerives;

namespace CRM.EndPoint.FrontEnd.Services.SetInterval {
    public class TimedHostedService : IHostedService, IDisposable {
        private readonly ILogger<TimedHostedService> _logger;
        private readonly IServiceScopeFactory serviceScopeFactory;

        private Timer _timerShowNotif;
        private Timer _timerPrefactors;
        private Timer _timerCarizo;
        private Timer _compareSepidarCrmFactors;

        public TimedHostedService(ILogger<TimedHostedService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            this.serviceScopeFactory = serviceScopeFactory;
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timerShowNotif = new Timer(_ => Task.Run(ShowNotif), null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
            // _timerPrefactors = new Timer(_ => Task.Run(CheckPrefactors), null, TimeSpan.Zero, TimeSpan.FromHours(2));
            ScheduleNextRun();
            _timerCarizo = new Timer(_ => Task.Run(CheckCarizo), null, TimeSpan.Zero, TimeSpan.FromHours(2));
            _compareSepidarCrmFactors = new Timer(_ => Task.Run(CompareSepidarCrmFactors), null, TimeSpan.Zero, TimeSpan.FromHours(24));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            // متوقف کردن تایمر
            _timerShowNotif?.Change(Timeout.Infinite, 0);
            _timerPrefactors?.Change(Timeout.Infinite, 0);
            _timerCarizo?.Change(Timeout.Infinite, 0);
            _compareSepidarCrmFactors?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timerShowNotif?.Dispose();
            _timerPrefactors?.Dispose();
            _timerCarizo?.Dispose();
            _compareSepidarCrmFactors?.Dispose();
        }


        private void ScheduleNextRun()
        {
            _timerPrefactors?.Dispose();
            var now = DateTime.Now;
            var midnight = now.Date.AddDays(1); // فردا ساعت 00:00
            var delay = midnight - now; // فاصله تا ۱۲ شب

            _timerPrefactors = new Timer(_ => {
                Task.Run(async () => {
                    try
                    {
                        await CheckPrefactors();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in CheckPrefactors: {ex.Message}");
                    }
                    finally
                    {
                        ScheduleNextRun();
                    }
                });
            }, null, delay, Timeout.InfiniteTimeSpan); // فقط یک بار اجرا
        }


        /// <summary>
        /// مقایسه مبلغ های ثبت شده در crm و سپیدار
        /// </summary>
        public async Task CompareSepidarCrmFactors()
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CRMContextDb>();
                var S1db = scope.ServiceProvider.GetRequiredService<Sepidar1ContextDb>();

                var crmFactors = db.Factors.AsNoTracking().Where(f => f.status == FactorStatus.تایید_شده && f.SepidarId != null).ToList();

                var joined = (from crm in crmFactors
                        join sepidar in S1db.vwInvoice.AsNoTracking()
                            on crm.SepidarId equals sepidar.InvoiceId
                        let crmInt = (long)crm.PaymentAmount
                        let sepInt = (long)sepidar.NetPrice
                        let flag = (crmInt / 10) != (sepInt / 10)
                        select new { crm, sepidar, flag })
                    .Where(f => f.flag).ToList();

                var warnings = joined
                    .Select(f => new SystemWarning {
                        Message = "مبلغ ثبت شده در فاکتور شماره " + f.crm.SerialNumber + " با فاکتور سپیدار آن مطابقت ندارند.",
                        EntityType = EntityTypeEnum.Factor,
                        EntityTypeTxt = nameof(EntityTypeEnum.Factor),
                        EntityId = f.crm.Id
                    })
                    .ToList();

                await db.SystemWarnings.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.SystemWarnings.AddRangeAsync(warnings);
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async void UpdateAllTask(object state)
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var hub = scope.ServiceProvider.GetRequiredService<IHubContext<TaskHub>>();

                // تمام تسک ها رو بروز میکنه
                await hub.Clients.All.SendAsync("RefreshTasks");
            }
        }

        private async Task ShowNotif()
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CRMContextDb>();
                var dts = scope.ServiceProvider.GetRequiredService<DateTimeServices>();
                var hub = scope.ServiceProvider.GetRequiredService<IHubContext<TaskHub>>();
                var pushQueue = scope.ServiceProvider.GetRequiredService<IPushQueue>();
                var _data = await db.Carizos.Include(f => f.TaskUsers).Where(f =>
                    f.TaskStatus == false
                    && f.Remind == true
                    && f.DateMiladi.Value <= DateTime.Now
                    && f.RemindDismiss == false
                ).ToListAsync();
                foreach (var item in _data)
                {
                    item.ElapsTime = dts.ElapsTime(item.DateMiladi.Value);

                    await hub.Clients.User(item.AssignUserId).SendAsync("ReceiveNotification", item);
                }

                /*if (_data.Count > 0)
                {
                    var usersWithTasks = _data
                        .Select(x => x.AssignUserId)
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Distinct();

                    foreach (var userId in usersWithTasks)
                    {
                        pushQueue.Enqueue(new PushQueueItem {
                            UserId = userId,
                            Title = "یادآوری کاریزو",
                            Body = "یک یا چند کاریزوی شما موعد آن سررسیده است.",
                            Url = "/Carizo/Index",
                            Tag = "carizo-remind"
                        });
                    }
                }*/
            }
        }


        /// <summary>
        /// دو تا کار انجام میده
        /// 1 پیش فاکتور هایی که تاریخ اعتبارشون گذشته رو فاقد اعتبار می کند
        /// 2 پیش فاکتور های فاقد اعتبار که از تاریخ فاقد اعتبارشون 15 روز گذشته رو حذف سیستمی میکنه
        /// </summary>
        /// <returns></returns>
        private async Task CheckPrefactors()
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CRMContextDb>();
                var dtService = scope.ServiceProvider.GetRequiredService<DateTimeServices>();
                var genService = scope.ServiceProvider.GetRequiredService<Service>();
                var logSystemService = scope.ServiceProvider.GetRequiredService<LogSystemService>();

                var _data = await (from items in db.PreFactors
                        where items.Accepted == false && items.Pftype != (int)PreFactorEnum.بسته_شده &&
                              items.Pftype != (int)PreFactorEnum.فاقد_اعتبار && items.Pftype != (int)PreFactorEnum.حذف_شده
                        select items)
                    .ToListAsync();

                foreach (var item in _data)
                {
                    if (item.ValidityDateMiladi == null)
                    {
                        item.ValidityDateMiladi = dtService.GetDateMiladi(item.ValidityDade);
                        item.DateMiladi = dtService.GetDateMiladi(item.Date);
                    }

                    if (item.ValidityDateMiladi < DateTime.Parse(DateTime.Now.ToShortDateString()))
                    {
                        var oldPf = await db.PreFactors.AsNoTracking().FirstOrDefaultAsync(f => f.Id == item.Id);

                        item.Accepted = false;
                        if (item.AcceptedTerms == true) item.AcceptedTerms = false;
                        item.PrePftype = item.Pftype;
                        item.Pftype = (int)PreFactorEnum.فاقد_اعتبار;
                        item.PftypeTxt = Service.GetTypeTxtPfactor(item.Pftype);


                        await logSystemService.LogChange(oldPf, item, LogActionEnum.تغییر_وضعیت, new AppUser { FirstName = "سیستم" });
                    }
                }

                await db.SaveChangesAsync();
                var ExpirePrefactors = await (from items in db.PreFactors
                        where items.Pftype == (int)PreFactorEnum.بسته_شده || items.Pftype == (int)PreFactorEnum.فاقد_اعتبار
                        select items)
                    .ToListAsync();
                foreach (var item in ExpirePrefactors)
                {
                    if (item.ValidityDateMiladi == null)
                    {
                        item.ValidityDateMiladi = dtService.GetDateMiladi(item.ValidityDade);
                        item.DateMiladi = dtService.GetDateMiladi(item.Date);
                    }

                    if (DateTime.Now >= item.ValidityDateMiladi.Value.AddDays(15))
                    {
                        await genService.DeletePreFactor(item, new AppUser { FirstName = "سیستم", Id = null });
                    }
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckPrefactors Schduler");
            }
        }

        /// <summary>
        /// تسک های انجام شده رو اگر 15 روز گذشته باشه مخفی میکنه و باید چشم رو بزنه و مجدد ببینه
        /// </summary>
        /// <returns></returns>
        private async Task CheckCarizo()
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CRMContextDb>();

                var carizList = await db.Carizos
                    .Where(c => c.TaskStatus && c.DateMiladi.HasValue)
                    .ToListAsync();

                foreach (var item in carizList)
                {
                    if (DateTime.Now >= item.DateMiladi.Value.AddDays(15))
                    {
                        item.TaskHide = true; //
                    }
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckCarizoAsync Schduler");
            }
        }


        private async Task CheckFactorAcc()
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CRMContextDb>();

                var carizList = await db.Carizos
                    .Where(c => c.TaskStatus && c.DateMiladi.HasValue)
                    .ToListAsync();

                foreach (var item in carizList)
                {
                    if (DateTime.Now >= item.DateMiladi.Value.AddDays(15))
                    {
                        item.TaskHide = true;
                    }
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckCarizoAsync Schduler");
            }
        }
    }
}