using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Carizos.Enum;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Enum;

namespace CRM.EndPoint.FrontEnd.Services.TaskServices {
    public class TaskServices {
        private readonly UserManager<AppUser> userManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly DateTimeServices dts;
        private readonly CRMContextDb db;
        private readonly UserContextDb udb;

        public TaskServices(UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor, DateTimeServices dts, CRMContextDb db,
            UserContextDb udb)
        {
            this.userManager = userManager;
            this.httpContextAccessor = httpContextAccessor;
            this.dts = dts;
            this.db = db;
            this.udb = udb;
        }

        public async Task<List<Carizo>> TaskReciveAmountPrefctor(Accountancy ac, InvoiceTypeEnum type) //1-prefactor 2-factor
        {
            var oldCarizos = await db.Carizos.Where(f => f.EntityId == ac.Id).ToListAsync();
            var isUpdatedAcc = false;
            if (oldCarizos.Count != 0)
            {
                isUpdatedAcc = true;
                db.Carizos.RemoveRange(oldCarizos);
            }

            //var user = userManager.GetUserAsync(httpContextAccessor.HttpContext.User).Result;
            var allMali = userManager.GetUsersInRoleAsync("کارشناس مالی").Result;


            var selAdmin = await udb.Users.Where(f => f.PositionId == Guid.Parse("36938255-50af-491f-b0df-a5c4079fefd8")).FirstOrDefaultAsync();

            if (type == InvoiceTypeEnum.پیش_فاکتور) //preFactor
            {
                var pf = await db.PreFactors.Where(f => f.Id == ac.pfid).FirstOrDefaultAsync();

                List<Carizo> carizos = [];

                var _Task = new Carizo {
                    Id = Guid.NewGuid(),
                    TaskType = TaskTypeEnum.اشتراکی,
                    TaskTypeTxt = TaskTypeEnum.اشتراکی.ToString(),
                    Remind = true,
                    RemindDismiss = true,
                    Repeat = true,
                    RepeatType = "daily",
                    RepeatTypeTxt = "هرروز",
                    TaskStatus = false,
                    AssignUser = selAdmin.FullName,
                    AssignUserId = selAdmin.Id,
                    UserCreator = selAdmin.FullName,
                    UserCreatorID = selAdmin.Id,
                    RepeatUntil = "همیشه",
                    EntityType = "Acc",
                    EntityId = ac.Id,
                    Systematic = true
                };


                _Task.ShareTaskId = _Task.Id;

                if (ac.AccType == AcEnum.واریزی) //AccEnum  واریزی
                {
                    switch (ac.PayType)
                    {
                        case PayTypeEnum.نقدی:
                            _Task.Title = !isUpdatedAcc ? "واریزی نقدی" : "اصلاحیه واریزی نقدی";
                            _Task.Description = "یک پرداختی بابت پیش فاکتور شماره " + "<span class='hoverLink' onclick='ShowPreviewPfactor(\"" +
                                                pf.Id + "\")'>" + pf.SerialNumber + "</span>" + " مشتری " +
                                                "<span class='hoverLink' onclick='openCustomerProfile(\"" + pf.CustomerId + "\")'>" + pf.AgentName +
                                                " " + pf.Customer + "</span>" + " توسط کارشناس " + "<b>" + ac.UserCreator + "</b>" + " ثبت شده است.";
                            _Task.Date = dts.GetDatePersian();
                            _Task.RemindTime = DateTime.Now.ToString("HH:mm");
                            _Task.DateMiladi = DateTime.Now;
                            _Task.Remind = true;
                            break;

                        case PayTypeEnum.تهاتری:
                            _Task.Title = !isUpdatedAcc ? "واریزی تهاتری" : "اصلاحیه واریزی تهاتری";
                            _Task.Description = "یک پرداختی بابت پیش فاکتور شماره " + "<span class='hoverLink' onclick='ShowPreviewPfactor(\"" +
                                                pf.Id + "\")'>" + pf.SerialNumber + "</span>" + " مشتری " +
                                                "<span class='hoverLink' onclick='openCustomerProfile(\"" + pf.CustomerId + "\")'>" + pf.AgentName +
                                                " " + pf.Customer + "</span>" + " توسط کارشناس " + "<b>" + ac.UserCreator + "</b>" + " ثبت شده است.";
                            _Task.Date = dts.GetDatePersian();
                            _Task.RemindTime = DateTime.Now.ToString("HH:mm");
                            _Task.DateMiladi = DateTime.Now;
                            _Task.Remind = true;
                            break;

                        case PayTypeEnum.چکی:
                            _Task.Title = !isUpdatedAcc ? "واریزی چک" : "اصلاحیه واریزی چک";
                            _Task.Description = "یک چک بابت پیش فاکتور شماره " + "<span class='hoverLink' onclick='ShowPreviewPfactor(\"" + pf.Id +
                                                "\")'>" + pf.SerialNumber + "</span>" + " مشتری " +
                                                "<span class='hoverLink' onclick='openCustomerProfile(\"" + pf.CustomerId + "\")'>" + pf.AgentName +
                                                " " + pf.Customer + "</span>" + " توسط کارشناس " + "<b>" + ac.UserCreator + "</b>" + " تاریخ وصول " +
                                                "<b>" + ac.VosolDate + "</b>" + " ثبت شده است.";
                            _Task.Date = ac.VosolDate;
                            _Task.RemindTime = "08:00";
                            _Task.DateMiladi = dts.GetDateMiladi(ac.VosolDate);
                            _Task.Remind = true;
                            _Task.RemindDismiss = false;
                            break;

                        case PayTypeEnum.اعتباری:
                            _Task.Title = !isUpdatedAcc ? "واریزی اعتباری" : "اصلاحیه واریزی اعتباری";
                            _Task.Description = "یک پرداختی اعتباری بابت پیش فاکتور شماره " +
                                                "<span class='hoverLink' onclick='ShowPreviewPfactor(\"" + pf.Id + "\")'>" + pf.SerialNumber +
                                                "</span>" + " مشتری " + "<span class='hoverLink' onclick='openCustomerProfile(\"" + pf.CustomerId +
                                                "\")'>" + pf.AgentName + " " + pf.Customer + "</span>" + " توسط کارشناس " + "<b>" + ac.UserCreator +
                                                "</b>" + " تاریخ وصول " + "<b>" + ac.VosolDate + "</b>" + " ثبت شده است.";
                            _Task.Date = ac.VosolDate;
                            _Task.RemindTime = "08:00";
                            _Task.DateMiladi = dts.GetDateMiladi(ac.VosolDate);
                            _Task.Remind = true;
                            _Task.RemindDismiss = false;
                            break;
                    }
                }

                if (ac.AccType == AcEnum.برگشتی) //AccEnum برگشتی
                {
                    switch (ac.PayType)
                    {
                        case PayTypeEnum.نقدی:
                            _Task.Title = !isUpdatedAcc ? "برگشتی نقدی" : "اصلاحیه برگشتی نقدی";
                            _Task.Description = "یک برگشتی بابت پیش فاکتور شماره " + "<span class='hoverLink' onclick='ShowPreviewPfactor(\"" +
                                                pf.Id + "\")'>" + pf.SerialNumber + "</span>" + " مشتری " +
                                                "<span class='hoverLink' onclick='openCustomerProfile(\"" + pf.CustomerId + "\")'>" + pf.AgentName +
                                                " " + pf.Customer + "</span>" + " توسط کارشناس " + "<b>" + ac.UserCreator + "</b>" + " ثبت شده است.";
                            _Task.Date = dts.GetDatePersian();
                            _Task.RemindTime = DateTime.Now.ToString("HH:mm");
                            _Task.DateMiladi = DateTime.Now;
                            _Task.Remind = true;
                            break;

                        case PayTypeEnum.تهاتری:
                            _Task.Title = !isUpdatedAcc ? "برگشتی تهاتری" : "اصلاحیه برگشتی تهاتری";
                            _Task.Description = "یک برگشتی بابت پیش فاکتور شماره " + "<span class='hoverLink' onclick='ShowPreviewPfactor(\"" +
                                                pf.Id + "\")'>" + pf.SerialNumber + "</span>" + " مشتری " +
                                                "<span class='hoverLink' onclick='openCustomerProfile(\"" + pf.CustomerId + "\")'>" + pf.AgentName +
                                                " " + pf.Customer + "</span>" + " توسط کارشناس " + "<b>" + ac.UserCreator + "</b>" + " ثبت شده است.";
                            _Task.Date = dts.GetDatePersian();
                            _Task.RemindTime = DateTime.Now.ToString("HH:mm");
                            _Task.DateMiladi = DateTime.Now;
                            _Task.Remind = true;
                            break;

                        case PayTypeEnum.چکی:
                            _Task.Title = !isUpdatedAcc ? "برگشتی چک" : "اصلاحیه برگشتی چک";
                            _Task.Description = "یک چک برگشتی بابت پیش فاکتور شماره " + "<span class='hoverLink' onclick='ShowPreviewPfactor(\"" +
                                                pf.Id + "\")'>" + pf.SerialNumber + "</span>" + " مشتری " +
                                                "<span class='hoverLink' onclick='openCustomerProfile(\"" + pf.CustomerId + "\")'>" + pf.AgentName +
                                                " " + pf.Customer + "</span>" + " توسط کارشناس " + "<b>" + ac.UserCreator + "</b>" + " تاریخ وصول " +
                                                "<b>" + ac.VosolDate + "</b>" + " ثبت شده است.";
                            _Task.Date = ac.VosolDate;
                            _Task.RemindTime = "08:00";
                            _Task.DateMiladi = dts.GetDateMiladi(ac.VosolDate);
                            _Task.Remind = true;
                            break;

                        case PayTypeEnum.اعتباری:
                            _Task.Title = !isUpdatedAcc ? "برگشتی اعتباری" : "اصلاحیه برگشتی اعتباری";
                            _Task.Description = "یک برگشتی اعتباری بابت پیش فاکتور شماره " +
                                                "<span class='hoverLink' onclick='ShowPreviewPfactor(\"" + pf.Id + "\")'>" + pf.SerialNumber +
                                                "</span>" + " مشتری " + "<span class='hoverLink' onclick='openCustomerProfile(\"" + pf.CustomerId +
                                                "\")'>" + pf.AgentName + " " + pf.Customer + "</span>" + " توسط کارشناس " + "<b>" + ac.UserCreator +
                                                "</b>" + " تاریخ وصول " + "<b>" + ac.VosolDate + "</b>" + " ثبت شده است.";
                            _Task.Date = ac.VosolDate;
                            _Task.RemindTime = "08:00";
                            _Task.DateMiladi = dts.GetDateMiladi(ac.VosolDate);
                            _Task.Remind = true;
                            break;
                    }
                }

                List<TaskUser> tu = [];

                foreach (var item in allMali)
                {
                    tu.Add(new TaskUser {
                        TaskId = _Task.Id,
                        UserId = Guid.Parse(item.Id),
                        FullName = item.FullName
                    });

                    var cl = _Task.Clone();
                    cl.Systematic = true;
                    cl.ShareTaskId = _Task.Id;
                    cl.AssignUser = item.FullName;
                    cl.AssignUserId = item.Id;

                    carizos.Add(cl);
                }

                if (ac.PayType == PayTypeEnum.اعتباری || ac.PayType == PayTypeEnum.چکی)
                {
                    if (!allMali.Select(f => f.Id).Contains(pf.UserCreatorID))
                    {
                        tu.Add(new TaskUser {
                            TaskId = _Task.Id,
                            UserId = Guid.Parse(pf.UserCreatorID),
                            FullName = pf.UserCreator
                        });

                        var cl2 = _Task.Clone();
                        cl2.Systematic = true;
                        cl2.ShareTaskId = _Task.Id;
                        cl2.AssignUser = pf.UserCreator;
                        cl2.AssignUserId = pf.UserCreatorID;

                        carizos.Add(cl2);
                    }
                }


                _Task.TaskUsers = tu;
                carizos.Add(_Task);

                return carizos;
            }

            return null;
        }
    }
}