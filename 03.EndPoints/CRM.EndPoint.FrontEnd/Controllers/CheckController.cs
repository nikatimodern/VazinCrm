using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Checks.Enums;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.EndPoint.FrontEnd.Models.Check;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Policy = "NotCustomerOnly")]
public class CheckController(
    CRMContextDb db,
    UserManager<AppUser> userManager,
    DateTimeServices dtService,
    LogSystemService logSystemService) : Controller {
    
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var checkbooks = await GetAllCheckbooks();
        return View(checkbooks);
    }

    public async Task<IActionResult> Received()
    {
        var checkbooks = await GetAllCheckbooks();
        return View(checkbooks);
    }


    public async Task<List<CheckbookVModel>> GetAllCheckbooks()
    {
        var checkbooks = await db.Checkbooks
            .AsNoTracking()
            .Include(f => f.BankAccount)
            .Select(c => new CheckbookVModel {
                Checkbook = c,
                ChecksCount = c.Checks.Count
            })
            .ToListAsync();

        return checkbooks;
    }


    public async Task<IActionResult> GetAllCheckbooksPartial()
    {
        var checkbooks = await GetAllCheckbooks();
        return PartialView(checkbooks);
    }

    [HttpGet]
    public async Task<IActionResult> GetCheckListByCheckbookId(Guid checkbookId)
    {
        var checks = await db.Checks
            .AsNoTracking()
            .Where(f => f.CheckbookId == checkbookId)
            .OrderByDescending(f => f.CreateDate)
            .ToListAsync();

        var model = new {
            data = checks,
            Statuses = Enum.GetValues<CheckStatus>()
                .Select(x => new {
                    Value = x,
                    Text = x.ToString().Replace("_", " ")
                })
        };
        return Ok(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddCheckbook([FromBody] Checkbook model)
    {
        if (model == null) return BadRequest();

        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            model.UserCreator = user.FullName;
            model.UserCreatorID = user.Id;

            await logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);

            await db.Checkbooks.AddAsync(model);
            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddCheck([FromBody] Check model)
    {
        if (model == null) return BadRequest();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var isCreate = model.Id == Guid.Empty;
            Check oldCheck = null;

            if (isCreate)
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
            } else
            {
                oldCheck = await db.Checks.AsNoTracking().FirstOrDefaultAsync(f => f.Id == model.Id);
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;
            }

            model.DueDateMiladi = dtService.GetDateMiladi(model.DueDate);

            db.Checks.Update(model);

            if (isCreate) await logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
            else await logSystemService.LogChange(oldCheck, model, LogActionEnum.ویرایش, user);

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    public async Task<IActionResult> ChangeStatus(Guid id, CheckStatus status)
    {
        if (id == Guid.Empty) return BadRequest();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var oldCheck = await db.Checks.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            var check = await db.Checks.FirstOrDefaultAsync(f => f.Id == id);

            check.ModifyDate = DateTime.Now;
            check.ModifyDateTxt = dtService.GetDateTimePersian();
            check.UserModify = user.FullName;
            check.UserModifyID = user.Id;
            check.Status = status;

            await logSystemService.LogChange(oldCheck, check, LogActionEnum.تغییر_وضعیت, user);

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    public async Task<IActionResult> GetCheck(Guid id)
    {
        try
        {
            var item = await db.Checks.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (item == null) return NotFound();

            return Ok(item);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }


    public async Task<IActionResult> DeleteCheck(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.Checks.FirstOrDefaultAsync(f => f.Id == id);
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

    public async Task<IActionResult> DeleteCheckbook(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.Checkbooks.Include(f => f.Checks).FirstOrDefaultAsync(f => f.Id == id);
            if (item == null) return NotFound();

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;

            foreach (var check in item.Checks ?? [])
            {
                check.IsDeleted = true;
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
}