using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Services.Factor;
using CRM.EndPoint.FrontEnd.Services.SaveChangeCustom;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.EndPoint.FrontEnd.Services.TaskServices;
using CRM.EndPoint.FrontEnd.Services;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Services.SmsServices;
using Microsoft.AspNetCore.Authorization;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public class TaminController : Controller
{
    private readonly CRMContextDb db;

    public TaminController(CrmSaveCahngeAsync crmSaveCahngeAsync, TaskServices ts, SepidarService sps, SmsService SmsServ,
            UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, Service GenService, FileSaver fileSaver,
            DateTimeServices dtService, CRMContextDb db, UserContextDb Udb, Sepidar1ContextDb S1db,
            IHubContext<TaskHub> hub)
    {
        this.db = db;
    }


    public IActionResult Index()
    {
        return View();
    }


    [HttpGet]
    
    public async Task<IActionResult> GetOpenOrder()
    {

        //var Orders = await db.Orders.AsNoTracking().Where(f => f.PreFactorItemId != null).ToListAsync();
        //var model = await db.PreFactorItems
        //    .Where(f => Orders.Select(f => f.PreFactorItemId).ToArray().Contains(f.Id))
        //    .Select(f=> new OpenOrderViewModel()
        //    {
        //        Name=f.SupplierName,
        //        Product=f.ProductSmallName ,
        //        UnitTxt=f.UnitTxt,
        //        Weight =f.WeightCount,
        //        TaminUnitAmount=f.TaminUnitAmount,
        //        TaminTotalAmount=f.TaminTotalAmount
        //    })
        //    .ToListAsync();


        //return Ok(model);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> GetOpenOrderDt()
    {
        //var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
        //var start = Convert.ToInt32(HttpContext.Request.Form["start"].FirstOrDefault());
        //var length = Convert.ToInt32(HttpContext.Request.Form["length"].FirstOrDefault());
        //var searchValue = HttpContext.Request.Form["search[value]"].FirstOrDefault()?.ToLower()?.Trim();

        //var Orders = await db.Orders.AsNoTracking()
        //    .Where(f => f.PreFactorItemId != null)
        //    .Select(f => f.PreFactorItemId.Value)
        //    .ToListAsync();

        //var query = db.PreFactorItems.AsNoTracking().Where(f => Orders.Contains(f.Id));

        //if (!string.IsNullOrEmpty(searchValue)) {
        //    query = query.Where(f =>
        //        f.SupplierName.Contains(searchValue) ||
        //        f.ProductSmallName.Contains(searchValue));
        //}

        //var recordsTotal = await db.PreFactorItems.Where(f => Orders.Contains(f.Id)).CountAsync();
        //var recordsFiltered = await query.CountAsync();

        //IQueryable<PreFactorItem> finalQuery = query.OrderByDescending(f => f.ModifyDate);

        //if (length != -1)
        //{
        //    finalQuery = finalQuery.Skip(start).Take(length);
        //}

        //var data = await finalQuery
        //    .Select(f => new
        //    {
        //        f.SupplierName,
        //        f.ProductSmallName,
        //        f.UnitTxt,
        //        f.WeightCount,
        //        f.TaminUnitAmount,
        //        f.TaminTotalAmount
        //    })
        //    .ToListAsync();

        //return Json(new
        //{
        //    draw,
        //    recordsTotal,
        //    recordsFiltered,
        //    data = data.Select(x => new
        //    {
        //        Name = x.SupplierName,
        //        Product = x.ProductSmallName,
        //        UnitTxt = x.UnitTxt,
        //        Weight = x.WeightCount,
        //        TaminUnitAmount = x.TaminUnitAmount,
        //        TaminTotalAmount = x.TaminTotalAmount
        //    })
        //});
        return Ok();
    }


}