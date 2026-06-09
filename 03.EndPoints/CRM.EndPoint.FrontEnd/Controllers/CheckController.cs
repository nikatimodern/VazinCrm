using System;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Checks.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Check;
using CRM.EndPoint.FrontEnd.Services;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class CheckController : Controller {
    private readonly CRMContextDb db;
    private readonly UserManager<AppUser> userManager;
    private readonly DateTimeServices dtServices;


    public CheckController(CRMContextDb db, UserManager<AppUser> userManager, DateTimeServices dtServices)
    {
        this.db = db;
        this.userManager = userManager;
        this.dtServices = dtServices;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCheckbooksPartial()
    {
        var checkbooks = await db.Checkbooks
            .AsNoTracking()
            .OrderByDescending(c => c.CreateDate)
            .Select(c => new CheckbookVModel {
                Checkbook = c,
                ChecksCount = c.Checks.Count
            })
            .ToListAsync();

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

        return Ok(checks);
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
            model.UserCreator = user.FullName;
            model.UserCreatorID = user.Id;
            model.DueDateMiladi = dtServices.GetDateMiladi(model.DueDate);

            await db.Checks.AddAsync(model);
            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}