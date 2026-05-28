using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Supply;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.QueryExtensions;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class CashController : Controller {
    private readonly CRMContextDb db;
    private readonly UserContextDb udb;
    private readonly UserManager<AppUser> userManager;
    private readonly DateTimeServices dtService;
    private readonly Service genService;
    private readonly LogSystemService logSystemService;
    private readonly FileSaver fileSaver;

    public CashController(CRMContextDb db, UserContextDb udb, UserManager<AppUser> userManager, DateTimeServices dtService, Service genService,
        LogSystemService logSystemService, FileSaver fileSaver)
    {
        this.db = db;
        this.udb = udb;
        this.userManager = userManager;
        this.dtService = dtService;
        this.genService = genService;
        this.logSystemService = logSystemService;
        this.fileSaver = fileSaver;
    }

    [Authorize(Roles = "مدیر ارشد,تنخواه دار,مدیر مالی,کارشناس مالی")]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public IActionResult Cashier()
    {
        return View();
    }

    [Authorize(Roles = "مدیر ارشد,تنخواه دار,مدیر مالی,کارشناس مالی")]
    [HttpPost]
    public async Task<IActionResult> AddCash([FromBody] Cash model)
    {
        if (model == null) return BadRequest();

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var isAdd = model.Id == Guid.Empty;
            Cash oldCash = null;

            if (isAdd)
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
            } else
            {
                oldCash = await db.Cashes.Where(f => f.Id == model.Id).AsNoTracking().FirstOrDefaultAsync();
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;
            }

            model.CostDateMiladi = dtService.GetDateMiladi(model.CostDate) ?? DateTime.Now;
            model.StatusTxt = model.Status.ToString().Replace("_", " ");

            db.Cashes.Update(model);
            if (!isAdd)
                db.Entry(model).Property(f => f.Serial).IsModified = false;

            await db.SaveChangesAsync();

            if (isAdd)
            {
                await logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
            } else
            {
                await logSystemService.LogChange(oldCash, model, LogActionEnum.ویرایش, user);
            }

            var attach = fileSaver.saveAttach(model.Attachments, _AttachType.Cash);
            foreach (var att in attach)
            {
                att.EntityId = model.Id;
            }

            db.Attachments.UpdateRange(attach);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return BadRequest(e.Message);
        }
    }

    [Authorize(Roles = "مدیر ارشد,مدیر مالی,کارشناس مالی")]
    [HttpPost]
    public async Task<IActionResult> AddCashier([FromBody] Cashier model)
    {
        if (model == null) return BadRequest();

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            if (model.Id == Guid.Empty)
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
                db.Cashiers.Add(model);
            } else
            {
                var data = await db.Cashiers.Where(f => f.Id == model.Id).FirstOrDefaultAsync();
                data.ModifyDate = DateTime.Now;
                data.ModifyDateTxt = dtService.GetDateTimePersian();
                data.UserModify = user.FullName;
                data.UserModifyID = user.Id;
                data.UserId = model.UserId;
                data.UserTxt = model.UserTxt;
            }

            var usr = await udb.Users.Where(f => f.Id == model.UserId).FirstOrDefaultAsync();
            await userManager.AddToRoleAsync(usr, "تنخواه دار");


            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return BadRequest(e.Message);
        }
    }

    public async Task<IActionResult> GetCash(Guid id)
    {
        try
        {
            var data = await db.Cashes.Where(f => f.Id == id).AsNoTracking()
                .Select(c => new {
                    Cash = c,
                    Attachments = db.Attachments
                        .Where(a => a.EntityId == c.Id && a._AttachType == _AttachType.Cash)
                        .ToList()
                })
                .FirstOrDefaultAsync();
            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    public async Task<IActionResult> ChangeStatusCash(Guid id, CashStatusEnum status)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var data = await db.Cashes.FirstOrDefaultAsync(f => f.Id == id);
            if (data == null) return NotFound();

            data.Status = status;
            data.StatusTxt = status.ToString().Replace("_", " ");

            data.ModifyDate = DateTime.Now;
            data.ModifyDateTxt = dtService.GetDateTimePersian();
            data.UserModify = user.FullName;
            data.UserModifyID = user.Id;

            await CashAccountancyDebit(data, user);

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    private async Task CashAccountancyDebit(Cash cash, AppUser user)
    {
        if (cash == null) return;

        var acc = await db.Accountancies.FirstOrDefaultAsync(x => x.CashId == cash.Id);

        if (cash.Status == CashStatusEnum.تایید_شده)
        {
            if (acc == null)
            {
                acc = new Accountancy {
                    CashId = cash.Id,
                    CashierId = Guid.Parse(cash.UserCreatorID),
                    InvoiceType = InvoiceTypeEnum.تنخواه,
                    InvoiceTypeTxt = nameof(InvoiceTypeEnum.تنخواه),
                    AccType = AcEnum.بدهکاری,
                    AccTypeTxt = nameof(AcEnum.بدهکاری),
                    debit = cash.Amount,
                    UserCreatorID = user.Id,
                    UserCreator = user.FullName,
                    Accepted = true,
                    AcceptedUserId = user.Id,
                    AcceptedUser = user.FullName,
                    PayType = PayTypeEnum.نقدی,
                    PayTypeTxt = nameof(PayTypeEnum.نقدی),
                    Status = AccStatusEnum.تایید_شده,
                };
                await db.AddAsync(acc);
            } else
            {
                acc.debit = cash.Amount;
                acc.UserModifyID = user.Id;
                acc.UserModify = user.FullName;
                acc.ModifyDate = DateTime.Now;
                acc.ModifyDateTxt = dtService.GetDateTimePersian();
            }
        } else
        {
            if (acc != null)
            {
                db.Accountancies.Remove(acc);
            }
        }
    }


    public async Task<IActionResult> ChangeStatusAllCashToPending()
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var cashes = await db.Cashes.Where(f =>
                f.UserCreatorID == user.Id &&
                f.Status == CashStatusEnum.ثبت_موقت
            ).ToListAsync();

            foreach (var cash in cashes)
            {
                cash.Status = CashStatusEnum.در_دست_تایید;
                cash.StatusTxt = nameof(CashStatusEnum.در_دست_تایید).Replace("_", " ");

                cash.ModifyDate = DateTime.Now;
                cash.ModifyDateTxt = dtService.GetDateTimePersian();
                cash.UserModify = user.FullName;
                cash.UserModifyID = user.Id;
            }

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    public async Task<IActionResult> GetCashier(Guid id)
    {
        try
        {
            var data = await db.Cashiers.Where(f => f.Id == id).AsNoTracking().FirstOrDefaultAsync();
            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    public async Task<IActionResult> GetAllBaseCashCenter()
    {
        try
        {
            var query = db.BaseCenterCashes.AsNoTracking();


            var data = await query.Select(f => new {
                value = f.Id,
                text = f.Title
            }).ToListAsync();

            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    public async Task<IActionResult> GetAllBaseCash()
    {
        try
        {
            var query = db.BaseCashes.AsNoTracking();


            var data = await query.Select(f => new {
                value = f.Id,
                text = f.CostTitle
            }).ToListAsync();

            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    public async Task<IActionResult> DeleteCash(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.Cashes.Where(f => f.Id == id).FirstOrDefaultAsync();
            if (item == null) return NotFound();

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;

            await logSystemService.LogChange(item, null, LogActionEnum.حذف, user);

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    public async Task<IActionResult> DeleteCashier(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.Cashiers.Where(f => f.Id == id).FirstOrDefaultAsync();
            if (item == null) return NotFound();

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;


            var usr = await udb.Users.FirstOrDefaultAsync(f => f.Id == item.UserId);
            await userManager.RemoveFromRoleAsync(usr, "تنخواه دار");


            await logSystemService.LogChange(item, null, LogActionEnum.حذف, user);

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }


    public async Task<IActionResult> GetAllCashesDt()
    {
        try
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
            var model = JsonConvert.DeserializeObject<filterVModel>(Request.Form["model"].FirstOrDefault());
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.ToLower().Trim();

            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);


            var query = db.Cashes.AsQueryable();

            var isManager = true;
            if (!roles.Contains("مدیر ارشد") && !roles.Contains("مدیر مالی") && !roles.Contains("کارشناس مالی"))
            {
                query = query.Where(f => f.UserCreatorID == user.Id);
                isManager = false;
            } else
            {
                query = query.Where(f => f.Status != CashStatusEnum.ثبت_موقت || f.UserCreatorID == user.Id);
            }


            if (!string.IsNullOrEmpty(searchValue))
            {
                var searchNumber = TextService.convertToInt(searchValue);
                query = query.Where(f =>
                    f.Amount == searchNumber ||
                    f.Serial == searchNumber ||
                    f.CostCenterTxt.Contains(searchValue) ||
                    f.CostTitleTxt.Contains(searchValue)
                );
            }

            if (model?.filt != null)
            {
                query = query.ApplyFilter(model, null, d => d.CreateDate, f => f.UserCreatorID);
            }

            query = model._type switch {
                1 => query.Where(f => f.Status == CashStatusEnum.در_دست_تایید),
                2 => query.Where(f => f.Status == CashStatusEnum.تایید_شده),
                _ => query
            };

            var recordsTotal = query.Count();


            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
            {
                var isAscending = sortColumnDirection == "asc";
                query = sortColumn switch {
                    "Serial" => isAscending
                        ? query.OrderBy(f => f.Serial)
                        : query.OrderByDescending(f => f.Serial),
                    "CreateDateTxt" => isAscending
                        ? query.OrderBy(f => f.CreateDate)
                        : query.OrderByDescending(f => f.CreateDate),
                    "CostTitleTxt" => isAscending
                        ? query.OrderBy(f => f.CostTitleTxt)
                        : query.OrderByDescending(f => f.CostTitleTxt),
                    "CostCenterTxt" => isAscending
                        ? query.OrderBy(f => f.CostCenterTxt)
                        : query.OrderByDescending(f => f.CostCenterTxt),
                    "CostDate" => isAscending
                        ? query.OrderBy(f => f.CostDateMiladi)
                        : query.OrderByDescending(f => f.CostDateMiladi),
                    "Amount" => isAscending
                        ? query.OrderBy(f => f.Amount)
                        : query.OrderByDescending(f => f.Amount),
                    "CostDescription" => isAscending
                        ? query.OrderBy(f => f.CostDescription)
                        : query.OrderByDescending(f => f.CostDescription),
                    "UserCreator" => isAscending
                        ? query.OrderBy(f => f.UserCreator)
                        : query.OrderByDescending(f => f.UserCreator),
                    _ => query
                };
            }


            if (length != -1)
                query = query.Skip(start).Take(length);

            var data = await query.ToListAsync();

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data,
                isManager,
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public async Task<IActionResult> GetAllCashiersDt()
    {
        try
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
            // var model = JsonConvert.DeserializeObject<filterVModel>(Request.Form["model"].FirstOrDefault());
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.ToLower().Trim();

            var cashiers = db.Cashiers.AsNoTracking();

            if (!string.IsNullOrEmpty(searchValue))
            {
                cashiers = cashiers.Where(x =>
                    x.UserTxt.Contains(searchValue) ||
                    x.Title.Contains(searchValue)
                );
            }

            if (!string.IsNullOrEmpty(sortColumn))
            {
                var asc = sortColumnDirection == "asc";
                cashiers = sortColumn switch {
                    "Title" => asc
                        ? cashiers.OrderBy(x => x.Title)
                        : cashiers.OrderByDescending(x => x.Title),

                    _ => cashiers.OrderByDescending(x => x.CreateDate)
                };
            }

            var recordsTotal = await cashiers.CountAsync();

            if (length != -1)
                cashiers = cashiers.Skip(start).Take(length);

            var data = await cashiers.ToListAsync();

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data,
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }


    public async Task<IActionResult> GetAllCashiers()
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var query = db.Cashiers.AsNoTracking();


            var data = await query.Select(f => new {
                value = f.UserId,
                text = f.UserTxt,
                target = f.UserId == user.Id
            }).ToListAsync();

            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}