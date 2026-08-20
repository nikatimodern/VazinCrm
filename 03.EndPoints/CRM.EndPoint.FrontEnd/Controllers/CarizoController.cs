using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Carizos.Enum;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.SaveChangeCustom;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.EndPoint.FrontEnd.IServices.PushNotification;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.Models.Carizo;
using CRM.EndPoint.FrontEnd.Models.PushNotification;
using CRM.EndPoint.FrontEnd.Services.Carizo;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.Infrastructures.Sql;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize(Policy = "NotCustomerOnly")]
    public class CarizoController : Controller {
        private readonly IViewRenderService vrs;

        //private readonly ChatHub chat;
        private readonly CrmSaveCahngeAsync crmSaveCahngeAsync;
        private readonly TaskServices ts;

        private readonly CRMContextDb db;
        private readonly UserContextDb udb;
        private readonly UserManager<AppUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly DateTimeServices dts;
        private readonly IHubContext<TaskHub> hub;
        private readonly Service genService;
        private readonly LogSystemService _logSystemService;
        private readonly IPushQueue pushQueue;

        public CarizoController(IViewRenderService vrs, CrmSaveCahngeAsync crmSaveCahngeAsync, TaskServices ts, CRMContextDb db, UserContextDb udb,
            UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, DateTimeServices dts, IHubContext<TaskHub> hub,
            Service GenService, LogSystemService logSystemService, IPushQueue pushQueue)
        {
            this.vrs = vrs;
            this.crmSaveCahngeAsync = crmSaveCahngeAsync;
            this.ts = ts;
            this.db = db;
            this.udb = udb;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.dts = dts;
            this.hub = hub;
            genService = GenService;
            _logSystemService = logSystemService;
            this.pushQueue = pushQueue;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> GetRemind()
        {
            try
            {
                var model = await getRem();
                var _html = await vrs.RenderToStringAsync("GetRemind", model);
                return Ok(new { count = model.Count, html = _html });
            }
            catch
            {
                return BadRequest();
            }
        }

        private async Task<List<Carizo>> getRem()
        {
            var _user = userManager.GetUserAsync(HttpContext.User).Result;
            var _data = await db.Carizos.Include(f => f.TaskUsers)
                .Where(f => f.AssignUserId == _user.Id
                            && f.TaskStatus == false
                            && f.Remind == true
                            && f.DateMiladi.Value <= DateTime.Now
                ).ToListAsync();

            foreach (var item in _data)
            {
                item.ElapsTime = dts.ElapsTime(item.DateMiladi.Value);
            }


            return _data;
        }


        public async Task<IActionResult> UpdateTaskTime(Guid id, string _Time)
        {
            try
            {
                var _user = userManager.GetUserAsync(HttpContext.User).Result;
                var oldData = await db.Carizos.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
                var data = await db.Carizos.Where(f => f.Id == id).FirstOrDefaultAsync();


                if (data.Date == string.Empty)
                {
                    data.Date = dts.GetDatePersian();
                    data.DateMiladi = dts.GetDateMiladi(data.Date);
                }

                data.RemindTime = _Time;
                TimeSpan t = TimeSpan.Parse(data.RemindTime);
                var Gdate = dts.GetDateMiladi(data.Date);
                data.DateMiladi = Gdate.Value.Date.Add(t);
                data.RemindDismiss = false;
                data.Remind = true;

                await _logSystemService.LogChange(oldData, data, LogActionEnum.ویرایش, _user);
                await crmSaveCahngeAsync.SaveChange();
                //await db.SaveChangesAsync();
                return Ok();
            }
            catch
            {
                return BadRequest();
            }
        }


        public async Task<IActionResult> AddSnooze(Guid id)
        {
            try
            {
                var data = await db.Carizos.Where(f => f.Id == id).FirstOrDefaultAsync();
                data.RemindDismiss = false;
                data.Remind = true;
                data.DateMiladi = DateTime.Now.AddMinutes(10);
                data.Date = dts.GetDatePersian(data.DateMiladi.Value);
                data.RemindTime = dts.GetDateTimePersian(data.DateMiladi.Value).Substring(11, 5);
                //await db.SaveChangesAsync();
                await crmSaveCahngeAsync.SaveChange();
                return Ok();
            }
            catch
            {
                return BadRequest();
            }
        }


        public async Task<IActionResult> TaskToggle(Guid id, int btnC)
        {
            var data = await db.Carizos.Where(f => f.Id == id).FirstOrDefaultAsync();
            var rem = data.Remind;
            var dismis = data.RemindDismiss;


            if (dismis == true && rem == false && btnC == 1)
            {
                data.RemindDismiss = true;
                data.Remind = true;
            } else if (dismis == true && rem == false && btnC == 2)
            {
                data.RemindDismiss = false;
                data.Remind = true;
            } else if (dismis == true && rem == true && btnC == 1)
            {
                data.RemindDismiss = true;
                data.Remind = false;
            } else if (dismis == true && rem == true && btnC == 2)
            {
                data.RemindDismiss = false;
                data.Remind = true;
            } else if (dismis == false && rem == true && btnC == 1)
            {
                data.RemindDismiss = true;
                data.Remind = false;
            } else if (dismis == false && rem == true && btnC == 2)
            {
                data.RemindDismiss = true;
                data.Remind = true;
            } else if (dismis == false && rem == false && btnC == 1)
            {
                data.RemindDismiss = false;
                data.Remind = true;
            } else if (dismis == false && rem == false && btnC == 2)
            {
                data.RemindDismiss = true;
                data.Remind = false;
            }


            await db.SaveChangesAsync();
            return Ok();
        }


        public async Task<IActionResult> DismissTask(Guid id)
        {
            var data = await db.Carizos.Where(f => f.Id == id).FirstOrDefaultAsync();
            if (data != null)
            {
                data.RemindDismiss = true;
                //await db.SaveChangesAsync();
                await crmSaveCahngeAsync.SaveChange();
            }

            return Ok();
        }


        public async Task<IActionResult> ShowTask(Guid id)
        {
            var data = await db.Carizos.Where(f => f.Id == id).FirstOrDefaultAsync();
            data.Remind = false;
            data.RemindDismiss = true;
            //await db.SaveChangesAsync();
            await crmSaveCahngeAsync.SaveChange();
            return Ok();
        }


        public async Task<IActionResult> UnCheckTask(Guid id)
        {
            var user = userManager.GetUserAsync(HttpContext.User).Result;

            var oldTa = await db.Carizos.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            var Ta = await db.Carizos.Where(f => f.Id == id).FirstOrDefaultAsync();


            if (Ta.TaskType == TaskTypeEnum.تکی)
            {
                Ta.TaskStatus = false;
            } else if (Ta.TaskType == TaskTypeEnum.اختصاصی)
            {
                if (Ta.UserCreatorID == user.Id)
                {
                    Ta.TaskStatus = false;
                } else
                {
                    var MasterTask = await db.Carizos.Include(f => f.TaskUsers).Where(f => f.Id == Ta.ShareTaskId).FirstOrDefaultAsync();
                    if (!MasterTask.TaskStatus)
                    {
                        Ta.TaskStatus = false;
                        MasterTask.TaskUsers.Where(f => f.UserId == Guid.Parse(Ta.AssignUserId)).FirstOrDefault().completed = false;
                    } else
                    {
                        return Ok(new { Status = 201, error = "این تسک توسط سازنده پایان یافته است", data = "" });
                    }
                }
            } else //TaskTypeEnum.اشتراکی
            {
                //var Parent = await db.Carizos.Include(d => d.TaskUsers).Where(f => f.Id == Guid.Parse(Ta.ShareTaskId)).FirstOrDefaultAsync();
                var AllTask = await db.Carizos.Include(d => d.TaskUsers).Where(f => f.ShareTaskId == Ta.ShareTaskId && f.Id != id).ToListAsync();
                foreach (var item in AllTask)
                {
                    item.TaskStatus = false;
                    item.CompleteTaskUser = null;
                    item.CompleteTaskUserId = null;
                    if (item.TaskUsers.Any()) //کسی که تسک یوزر داره پرند هست و سازنده می باشد
                    {
                        item.TaskUsers.Where(f => f.UserId == Guid.Parse(user.Id)).FirstOrDefault().completed = false;
                    }
                }

                Ta.TaskStatus = false;
                Ta.CompleteTaskUser = null;
                Ta.CompleteTaskUserId = null;
            }


            await _logSystemService.LogChange(oldTa, Ta, LogActionEnum.ویرایش, user);
            //await db.SaveChangesAsync();
            await crmSaveCahngeAsync.SaveChange();
            return Ok(new { Status = 200, error = "", data = "" });
            ;
        }

        public async Task<IActionResult> FinishTask(Guid id)
        {
            var user = userManager.GetUserAsync(HttpContext.User).Result;

            var oldTa = await db.Carizos.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            var Ta = await db.Carizos.Where(f => f.Id == id).FirstOrDefaultAsync();


            if (Ta.TaskType == TaskTypeEnum.تکی)
            {
                Ta.TaskStatus = true;
                Ta.CompleteTaskUser = user.FullName;
                Ta.CompleteTaskUserId = user.Id;
            } else if (Ta.TaskType == TaskTypeEnum.اختصاصی)
            {
                var Parent = await db.Carizos.Include(d => d.TaskUsers).Where(f => f.Id == Ta.ShareTaskId).FirstOrDefaultAsync();

                if (Parent.UserCreatorID == user.Id)
                {
                    if (Parent.TaskUsers.Where(f => f.completed == false).Any())
                        return Ok(new { Status = 201, error = "کاربرانی در حال انجام این تسک هستند", data = "" });
                    else
                    {
                        Ta.TaskStatus = true;
                        Ta.CompleteTaskUser = user.FullName;
                        Ta.CompleteTaskUserId = user.Id;
                    }
                } else
                {
                    Parent.TaskUsers.Where(f => f.UserId == Guid.Parse(user.Id)).FirstOrDefault().completed = true;
                    Ta.TaskStatus = true;
                    Ta.CompleteTaskUser = user.FullName;
                    Ta.CompleteTaskUserId = user.Id;
                }
            } else //TaskTypeEnum.اشتراکی
            {
                //var Parent = await db.Carizos.Include(d => d.TaskUsers).Where(f => f.Id == Guid.Parse(Ta.ShareTaskId)).FirstOrDefaultAsync();
                var AllTask = await db.Carizos.Include(d => d.TaskUsers).Where(f => f.ShareTaskId == Ta.ShareTaskId && f.Id != id).ToListAsync();
                foreach (var item in AllTask)
                {
                    item.TaskStatus = true;
                    item.CompleteTaskUser = user.FullName;
                    item.CompleteTaskUserId = user.Id;
                    if (item.TaskUsers.Any()) //کسی که تسک یوزر داره پرند هست و سازنده می باشد
                    {
                        item.TaskUsers.Where(f => f.UserId == Guid.Parse(user.Id)).FirstOrDefault().completed = true;
                    }
                }

                Ta.TaskStatus = true;
                Ta.CompleteTaskUser = user.FullName;
                Ta.CompleteTaskUserId = user.Id;
            }


            await _logSystemService.LogChange(oldTa, Ta, LogActionEnum.ویرایش, user);
            //await db.SaveChangesAsync();
            await crmSaveCahngeAsync.SaveChange();
            return Ok();
        }


        public async Task<IActionResult> DelTask(Guid id)
        {
            var user = userManager.GetUserAsync(HttpContext.User).Result;
            var data = await db.Carizos.Where(f => f.Id == id).FirstOrDefaultAsync();
            await _logSystemService.LogChange(data, null, LogActionEnum.حذف, user);

            if (data.TaskType != TaskTypeEnum.تکی && data.UserCreatorID == user.Id)
            {
                var alltask = await db.Carizos.Where(f => f.ShareTaskId == data.Id).ToListAsync();
                foreach (var item in alltask)
                {
                    item.IsDeleted = true;
                }

                data.IsDeleted = true;
            } else
                data.IsDeleted = true;


            //await db.SaveChangesAsync();
            await crmSaveCahngeAsync.SaveChange();
            return Ok();
        }


        private async Task<Carizo> GenModel(Carizo model, AppUser _user)
        {
            if (model.Repeat == true && model.Remind == true)
            {
                if (model.Date == string.Empty)
                {
                    model.Date = dts.GetDatePersian();
                    model.DateMiladi = DateTime.Now;
                }

                TimeSpan time = TimeSpan.Parse(TextService.ToEnglishNumber(model.RemindTime));
                DateTime? d = dts.GetDateMiladi(model.Date);
                model.DateMiladi = d.Value.Date.Add(time);
            } else if (model.Repeat == true)
            {
                if (model.Date == string.Empty)
                {
                    model.Date = dts.GetDatePersian();
                    model.DateMiladi = DateTime.Now.Date;
                } else
                {
                    model.DateMiladi = dts.GetDateMiladi(TextService.ToEnglishNumber(model.Date));
                }
            } else if (model.Remind == true)
            {
                if (model.Date == string.Empty)
                {
                    model.Date = dts.GetDatePersian();
                }

                TimeSpan t = TimeSpan.Parse(TextService.ToEnglishNumber(model.RemindTime));
                DateTime? d = dts.GetDateMiladi(TextService.ToEnglishNumber(model.Date));
                model.DateMiladi = d.Value.Date.Add(t);
            } else
            {
                if (model.Date != string.Empty)
                    model.DateMiladi = dts.GetDateMiladi(model.Date);
            }

            switch (model.RepeatType)
            {
                case "daily":
                    model.RepeatTypeTxt = "هرروز";
                    break;
                case "weekly":
                    model.RepeatTypeTxt = "هرهفته";
                    break;
                case "monthly":
                    model.RepeatTypeTxt = "هرماه";
                    break;
                case "custom":
                    model.RepeatTypeTxt = "سفارشی";
                    break;
            }


            if (model.Id == Guid.Empty)
            {
                model.UserCreator = _user.FullName;
                model.UserCreatorID = _user.Id;

                model.AssignUser = _user.FullName;
                model.AssignUserId = _user.Id;
                model.ShareTaskId = model.Id;
            } else
            {
                var preTaskUp = await db.Carizos.AsNoTracking().Where(f => f.Id == model.Id).FirstOrDefaultAsync();

                model.UserCreator = preTaskUp.UserCreator;
                model.UserCreatorID = preTaskUp.UserCreatorID;

                model.AssignUser = preTaskUp.AssignUser;
                model.AssignUserId = preTaskUp.AssignUserId;
                model.ShareTaskId = preTaskUp.ShareTaskId;
            }

            model.ShareTaskId = model.Id;

            return model;
        }


        [HttpPost]
        public async Task<IActionResult> AddTask([FromBody] Carizo _model)
        {
            try
            {
                var _user = userManager.GetUserAsync(HttpContext.User).Result;

                if (_model.EntityType == "Deal")
                {
                    var deal = await db.Deals.Where(f => f.Id == _model.EntityId).FirstOrDefaultAsync();
                    if (deal != null)
                    {
                        deal.ModifyDate = DateTime.Now;
                        deal.ModifyDateTxt = dts.GetDateTimePersian();
                    }
                }

                var _GenModel = await GenModel(_model, _user);

                //if (!_GenModel.TaskUsers.Where(f => f.IsDeleted == false).Any())
                //    _GenModel.TaskType = TaskTypeEnum.تکی;

                if (_model.Id == Guid.Empty)
                {
                    _model.Id = Guid.NewGuid();
                    switch (_model.TaskType)
                    {
                        case TaskTypeEnum.تکی: //1
                            //var model1 =await GenModel(_model, _user);
                            await db.Carizos.AddAsync(_GenModel);
                            await _logSystemService.LogChange(_GenModel, null, LogActionEnum.ثبت, _user);
                            break;

                        default: //TaskTypeEnum.اختصاصی://2   TaskTypeEnum.اشتراکی://3
                            var model2 = _GenModel;
                            model2.Id = Guid.NewGuid();
                            //model2.AssignUser = _user.FullName;
                            //model2.AssignUserId = _user.Id;
                            model2.ShareTaskId = model2.Id;
                            db.Carizos.Add(model2);


                            IList<Carizo> list = new List<Carizo>();
                            if (list == null) throw new ArgumentNullException(nameof(list));
                            foreach (var item in model2.TaskUsers)
                            {
                                Carizo CModel = model2.Clone();
                                CModel.AssignUser = item.FullName;
                                CModel.AssignUserId = item.UserId.ToString();
                                CModel.TaskUsers = null;
                                CModel.ShareTaskId = model2.Id;
                                CModel.UserCreator = model2.UserCreator;
                                CModel.UserCreatorID = model2.UserCreatorID;
                                list.Add(CModel);
                                await hub.Clients.User(CModel.AssignUserId).SendAsync("NotifTask",
                                    $"فعالیت <b>{CModel.Title}</b> توسط <b>{_user.FullName}</b> برای شما ثبت شد"
                                );

                                pushQueue.Enqueue(new PushQueueItem {
                                    UserId = CModel.AssignUserId,
                                    Title = "کاریزوی جدید",
                                    Body = $"یک فعالیت جدید توسط {_user.FirstName} در کاریزوی شما ثبت شد.",
                                    Url = "/Carizo/Index",
                                    Tag = "carizo-assign" + model2.Id
                                });
                            }

                            await db.Carizos.AddRangeAsync(list);

                            await _logSystemService.LogChange(model2, null, LogActionEnum.ثبت, _user);
                            break;

                        //case TaskTypeEnum.اشتراکی://3


                        //    break;
                    }
                } else
                {
                    var oldModel = await db.Carizos.AsNoTracking().Where(f => f.Id == _model.Id).FirstOrDefaultAsync();

                    if (_GenModel.UserCreatorID == _user.Id)
                    {
                        switch (_model.TaskType)
                        {
                            case TaskTypeEnum.تکی: //1
                                var model1 = _GenModel;

                                db.Carizos.Update(model1);
                                break;

                            default: //TaskTypeEnum.اختصاصی://2   TaskTypeEnum.اشتراکی://3
                                var model2 = _GenModel;
                                foreach (var item in _GenModel.TaskUsers)
                                {
                                    if (item.IsDeleted)
                                    {
                                        var dTask = await db.Carizos
                                            .Where(f => f.ShareTaskId == _GenModel.Id && f.AssignUserId == item.UserId.ToString())
                                            .FirstOrDefaultAsync();
                                        dTask.IsDeleted = true;
                                        db.Carizos.Remove(dTask);

                                        await hub.Clients.User(item.UserId.ToString()).SendAsync("NotifTask",
                                            $"کار <b>{dTask.Title}</b> توسط <b>{_user.FullName}</b> حذف شد");
                                    } else if (item.Id == Guid.Empty)
                                    {
                                        Carizo CModel = model2.Clone();
                                        CModel.AssignUser = item.FullName;
                                        CModel.AssignUserId = item.UserId.ToString();
                                        CModel.TaskUsers = null;
                                        CModel.ShareTaskId = model2.Id;
                                        CModel.UserCreator = model2.UserCreator;
                                        CModel.UserCreatorID = model2.UserCreatorID;

                                        db.Carizos.Add(CModel);
                                        await hub.Clients.User(CModel.AssignUserId).SendAsync("NotifTask",
                                            $"کار <b>{CModel.Title}</b> توسط <b>{_user.FullName}</b> برای شما ثبت شد");
                                        pushQueue.Enqueue(new PushQueueItem {
                                            UserId = CModel.AssignUserId,
                                            Title = "کاریزوی جدید",
                                            Body = $"یک فعالیت جدید توسط {_user.FirstName} در کاریزوی شما ثبت شد.",
                                            Url = "/Carizo/Index",
                                            Tag = "carizo-assign" + model2.Id
                                        });
                                    } else
                                    {
                                        var dTask = await db.Carizos
                                            .Where(f => f.ShareTaskId == _GenModel.Id && f.AssignUserId == item.UserId.ToString())
                                            .FirstOrDefaultAsync();
                                        dTask.IsDeleted = true;

                                        Carizo CModel = model2.Clone();
                                        CModel.AssignUser = item.FullName;
                                        CModel.AssignUserId = item.UserId.ToString();
                                        CModel.TaskUsers = null;
                                        CModel.ShareTaskId = model2.Id;
                                        CModel.UserCreator = model2.UserCreator;
                                        CModel.UserCreatorID = model2.UserCreatorID;
                                        CModel.TaskStatus = dTask.TaskStatus;

                                        db.Carizos.Add(CModel);
                                        await hub.Clients.User(item.UserId.ToString()).SendAsync("NotifTask",
                                            $"کار <b>{CModel.Title}</b> توسط <b>{_user.FullName}</b> تغییر کرد");
                                        pushQueue.Enqueue(new PushQueueItem {
                                            UserId = CModel.AssignUserId,
                                            Title = "تغییر کاریزو",
                                            Body = $"فعالیت {CModel.Title} توسط {_user.FullName} تغییر کرد",
                                            Url = "/Carizo/Index",
                                            Tag = "carizo-assign" + model2.Id
                                        });
                                    }
                                }

                                if (!model2.TaskUsers.Where(f => f.IsDeleted != true).Any())
                                {
                                    model2.TaskType = TaskTypeEnum.تکی;
                                    model2.TaskTypeTxt = TaskTypeEnum.تکی.ToString();
                                }

                                db.Carizos.Update(model2);

                                break;
                        }
                    } else
                    {
                        db.Carizos.Update(_GenModel);
                    }


                    await _logSystemService.LogChange(oldModel, _GenModel, LogActionEnum.ویرایش, _user);
                }


                //await db.SaveChangesAsync();
                await crmSaveCahngeAsync.SaveChange();


                return Ok(new { status = "200", error = "", data = "" });
            }
            catch (Exception e)
            {
                return Ok(new { Status = "500", error = e.Message, data = "" });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAllTask(int type, int loc, int systematic)
        {
            try
            {
                var user = userManager.GetUserAsync(HttpContext.User).Result;

                var currentDate = DateTime.Now;
                IQueryable<Carizo> dataQ = null;
                var _data = new List<Carizo>();

                switch (systematic)
                {
                    case 0:
                        dataQ = db.Carizos.Include(f => f.TaskUsers)
                            .AsQueryable();
                        break;
                    case 1:
                        dataQ = db.Carizos.Include(f => f.TaskUsers)
                            .Where(f => f.Systematic)
                            .AsQueryable();

                        break;
                    case 2:
                        dataQ = db.Carizos.Include(f => f.TaskUsers)
                            .Where(f => !f.Systematic)
                            .AsQueryable();
                        break;
                }

                dataQ = dataQ.OrderByDescending(f => f.DateMiladi).ThenByDescending(f => f.CreateDate);
                switch (type)
                {
                    case 0:
                        _data = await dataQ
                            .Where(f =>
                                f.AssignUserId == user.Id
                                && f.TaskStatus == false
                                && (f.DateMiladi.Value.Date > currentDate.Date || f.DateMiladi == null)
                            ).ToListAsync();
                        break;
                    case 1:
                        _data = await dataQ
                            .Where(f =>
                                f.AssignUserId == user.Id
                                && f.TaskStatus == false
                                && f.DateMiladi.Value.Date <= currentDate.Date
                            ).ToListAsync();
                        break;
                    case 2:
                        _data = await dataQ.Where(f =>
                            f.AssignUserId == user.Id
                            && f.TaskStatus == true
                            && f.TaskHide == false
                        ).ToListAsync();
                        break;
                    case 3:
                        _data = await dataQ.Where(f =>
                            f.AssignUserId == user.Id
                            && f.TaskStatus == true
                            && f.TaskHide == true
                        ).Take(300).ToListAsync();
                        break;
                }


                var model = new CarizoViewModel { CurrentUser = user, Data = _data, _Type = type, loc = loc };
                var html = await vrs.RenderToStringAsync("GetAllTask", model);

                return Ok(new { count = model.Data.Count, html });
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTask(Guid id)
        {
            try
            {
                //var _user = userManager.GetUserAsync(HttpContext.User).Result;
                var _data = await db.Carizos.Include(f => f.TaskUsers).Where(f => f.Id == id).FirstOrDefaultAsync();
                return Ok(_data);
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}