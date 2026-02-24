using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Supply;
using CRM.EndPoint.FrontEnd.Services;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی, تنخواه دار")]
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

    [Authorize(Roles = "مدیر ارشد, تنخواه دار, مدیر مالی, کارشناس مالی")]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public IActionResult Cashier()
    {
        return View();
    }

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
                var data = await db.Cashiers.Include(f => f.CashierUsers).Where(f => f.Id == model.Id).FirstOrDefaultAsync();
                data.ModifyDate = DateTime.Now;
                data.ModifyDateTxt = dtService.GetDateTimePersian();
                data.UserModify = user.FullName;
                data.UserModifyID = user.Id;
                data.CashierUsers = model.CashierUsers;
                data.Title = model.Title;
            }

            foreach (var cu in model.CashierUsers)
            {
                var usr = await udb.Users.Where(f => f.Id == cu.UserId).FirstOrDefaultAsync();
                await userManager.AddToRoleAsync(usr, "تنخواه دار");
            }


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
            var data = await db.Cashes.Where(f => f.Id == id).FirstOrDefaultAsync();
            if (data == null) return NotFound();

            data.Status = status;
            data.StatusTxt = status.ToString().Replace("_", " ");

            data.ModifyDate = DateTime.Now;
            data.ModifyDateTxt = dtService.GetDateTimePersian();
            data.UserModify = user.FullName;
            data.UserModifyID = user.Id;
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
            var data = await db.Cashiers.Where(f => f.Id == id).Include(f => f.CashierUsers).AsNoTracking().FirstOrDefaultAsync();
            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    public async Task<IActionResult> GetAllMyCashiers()
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);

            var query = db.Cashiers.AsNoTracking();

            if (!roles.Contains("مدیر ارشد") && !roles.Contains("مدیر مالی") && !roles.Contains("کارشناس مالی"))
            {
                query = query.Where(f => f.CashierUsers.Any(f => f.UserId == user.Id));
            }

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
            var item = await db.Cashiers.Where(f => f.Id == id).Include(c => c.CashierUsers).FirstOrDefaultAsync();
            if (item == null) return NotFound();

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;

            foreach (var cu in item.CashierUsers)
            {
                var userHasCashier = await db.CashierUsers.AnyAsync(x => x.UserId == cu.UserId && x.CashierId != item.Id);

                if (!userHasCashier)
                {
                    var usr = await udb.Users.FirstOrDefaultAsync(f => f.Id == cu.UserId);
                    await userManager.RemoveFromRoleAsync(usr, "تنخواه دار");
                }
            }

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
            //var model = JsonConvert.DeserializeObject<filterVModel>(Request.Form["model"].FirstOrDefault());
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.ToLower().Trim();

            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);


            var cashier = db.Cashiers.Include(f => f.CashierUsers.Where(f => f.UserId == user.Id)).AsNoTracking().Select(f => f.Id).ToList();
            var query = db.Cashes
                .Join(db.Cashiers,
                    c => c.CashierId,
                    ci => ci.Id,
                    (c, ci) => new { cash = c, CostCenter = ci.Title }
                );

            var isManager = true;
            if (!roles.Contains("مدیر ارشد") && !roles.Contains("مدیر مالی") && !roles.Contains("کارشناس مالی"))
            {
                query = query.Where(f => cashier.Contains(f.cash.CashierId));
                isManager = false;
            } else
            {
                query = query.Where(f => f.cash.Status != CashStatusEnum.ثبت_موقت || f.cash.UserCreatorID == user.Id);
            }


            if (!string.IsNullOrEmpty(searchValue))
            {
                var searchNumber = TextService.convertToInt(searchValue);
                query = query.Where(f =>
                    f.cash.Amount == searchNumber ||
                    f.cash.Serial == searchNumber  ||
                    f.cash.CostTitle.Contains(searchValue) ||
                    f.CostCenter.Contains(searchValue)
                );
            }

            var recordsTotal = query.Count();


            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
            {
                var isAscending = sortColumnDirection == "asc";
                query = sortColumn switch {
                    "Serial" => isAscending
                        ? query.OrderBy(f => f.cash.Serial)
                        : query.OrderByDescending(f => f.cash.Serial),
                    "Amount" => isAscending
                        ? query.OrderBy(f => f.cash.Amount)
                        : query.OrderByDescending(f => f.cash.Amount),
                    "CostTitle" => isAscending
                        ? query.OrderBy(f => f.cash.CostTitle)
                        : query.OrderByDescending(f => f.cash.CostTitle),
                    "CostCenter" => isAscending
                        ? query.OrderBy(f => f.CostCenter)
                        : query.OrderByDescending(f => f.CostCenter),
                    "CostDate" => isAscending
                        ? query.OrderBy(f => f.cash.CostDateMiladi)
                        : query.OrderByDescending(f => f.cash.CostDateMiladi),
                    _ => query
                };
            }


            if (length != -1)
                query = query.Skip(start).Take(length);

            var data = await query.OrderByDescending(f => f.cash.CreateDate).ToListAsync();

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

            var usersDict = await udb.Users
                .Where(u => u.IsEmployee)
                .Select(u => new { u.Id, u.FullName })
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            var cashiers = await db.Cashiers.AsNoTracking().Include(c => c.CashierUsers).ToListAsync();

            var merged = cashiers.Select(c => new {
                Cashier = c,
                UserFullNames = c.CashierUsers.Select(cu => usersDict.GetValueOrDefault(cu.UserId)).ToList()
            });


            if (!string.IsNullOrEmpty(searchValue))
            {
                merged = merged.Where(x =>
                    x.UserFullNames.Contains(searchValue) ||
                    x.Cashier.Title.Contains(searchValue)
                );
            }

            if (!string.IsNullOrEmpty(sortColumn))
            {
                var asc = sortColumnDirection == "asc";
                merged = sortColumn switch {
                    "Title" => asc
                        ? merged.OrderBy(x => x.Cashier.Title)
                        : merged.OrderByDescending(x => x.Cashier.Title),

                    _ => merged.OrderByDescending(x => x.Cashier.CreateDate)
                };
            }

            var recordsTotal = merged.Count();

            if (length != -1)
                merged = merged.Skip(start).Take(length);

            var data = merged.ToList();

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
}