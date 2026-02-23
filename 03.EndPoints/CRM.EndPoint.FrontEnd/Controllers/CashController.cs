using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
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

public class CashController : Controller {
    private readonly CRMContextDb db;
    private readonly UserContextDb udb;
    private readonly UserManager<AppUser> userManager;
    private readonly DateTimeServices dtService;
    private readonly Service genService;
    private readonly LogSystemService logSystemService;

    public CashController(CRMContextDb db, UserContextDb udb, UserManager<AppUser> userManager, DateTimeServices dtService, Service genService,
        LogSystemService logSystemService)
    {
        this.db = db;
        this.udb = udb;
        this.userManager = userManager;
        this.dtService = dtService;
        this.genService = genService;
        this.logSystemService = logSystemService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [Authorize(Roles = "مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
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

            var cashierId = await db.Cashiers.Where(f => f.UserId == user.Id).Select(f => f.Id).FirstOrDefaultAsync();
            if (cashierId == Guid.Empty)
                return NotFound("نام کاربری شما جز لیست تنخواه دارها ثبت نشده است!");


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
            model.CashierId = cashierId;
            model.CashierTxt = user.FullName;
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
            } else
            {
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;
            }

            db.Cashiers.Update(model);

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
            var data = await db.Cashes.Where(f => f.Id == id).AsNoTracking().FirstOrDefaultAsync();
            return Ok(data);
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

            var query = db.Cashes.OrderByDescending(f => f.CreateDate).AsNoTracking();
            if (!roles.Contains("مدیر ارشد") && !roles.Contains("مدیر مالی") && !roles.Contains("مدیر اجرایی") && !roles.Contains("مدیر عامل"))
            {
                query = query.Where(f => f.UserCreator == user.Id);
            }

            if (!string.IsNullOrEmpty(searchValue))
            {
                var searchNumber = TextService.convertToInt(searchValue);
                query = query.Where(f =>
                    f.Amount == searchNumber ||
                    f.Serial == searchNumber ||
                    f.CashierTxt.Contains(searchValue) ||
                    f.CostTitle.Contains(searchValue) ||
                    f.CostCenter.Contains(searchValue)
                );
            }

            var recordsTotal = query.Count();


            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
            {
                var isAscending = sortColumnDirection == "asc";
                query = sortColumn switch {
                    "Serial" => isAscending
                        ? query.OrderBy(f => f.Serial)
                        : query.OrderByDescending(f => f.Serial),
                    "Amount" => isAscending
                        ? query.OrderBy(f => f.Amount)
                        : query.OrderByDescending(f => f.Amount),
                    "CostTitle" => isAscending
                        ? query.OrderBy(f => f.CostTitle)
                        : query.OrderByDescending(f => f.CostTitle),
                    "CostCenter" => isAscending
                        ? query.OrderBy(f => f.CostCenter)
                        : query.OrderByDescending(f => f.CostCenter),
                    "CostDate" => isAscending
                        ? query.OrderBy(f => f.CostDateMiladi)
                        : query.OrderByDescending(f => f.CostDateMiladi),
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

            var cashiers = await db.Cashiers.AsNoTracking().ToListAsync();

            var merged = cashiers.Select(c => new {
                Cashier = c,
                UserFullName = usersDict.GetValueOrDefault(c.UserId)
            });

            if (!string.IsNullOrEmpty(searchValue))
            {
                merged = merged.Where(x =>
                    x.UserFullName.Contains(searchValue) ||
                    x.Cashier.Title.Contains(searchValue)
                );
            }

            if (!string.IsNullOrEmpty(sortColumn))
            {
                var asc = sortColumnDirection == "asc";
                merged = sortColumn switch {
                    "UserFullName" => asc
                        ? merged.OrderBy(x => x.UserFullName)
                        : merged.OrderByDescending(x => x.UserFullName),
                    "Title" => asc
                        ? merged.OrderBy(x => x.Cashier.Title)
                        : merged.OrderByDescending(x => x.Cashier.Title),
                    "CreateDateTxt" => asc
                        ? merged.OrderBy(x => x.Cashier.CreateDate)
                        : merged.OrderByDescending(x => x.Cashier.CreateDate),
                    _ => merged
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
                data
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}