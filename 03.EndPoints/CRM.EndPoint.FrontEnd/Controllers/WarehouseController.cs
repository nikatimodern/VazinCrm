using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.EndPoint.FrontEnd.Models.Warehouse;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.Framework;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Policy = "NotCustomerOnly")]
public class WarehouseController(CRMContextDb db, UserManager<AppUser> userManager, LogSystemService logSystemService) : Controller {
    public async Task<IActionResult> Index()
    {
        var warehouses = await GetAllWarehouses();
        return View(warehouses);
    }

    public async Task<List<WarehousVModel>> GetAllWarehouses()
    {
        var warehouses = await db.Warehouses
            .AsNoTracking()
            .Select(c => new WarehousVModel {
                Warehouse = c,
                WarehouseProductsCount = c.WarehouseProducts.Count
            })
            .ToListAsync();

        return warehouses;
    }


    public async Task<IActionResult> GetAllWarehousesPartial()
    {
        var warehouses = await GetAllWarehouses();
        return PartialView(warehouses);
    }

    [HttpPost]
    public async Task<IActionResult> AddWarehouse([FromBody] Warehouse model)
    {
        if (model == null) return BadRequest();

        var user = await userManager.GetUserAsync(HttpContext.User);
        model.UserCreatorID = user.Id;
        model.UserCreator = user.FullName;

        db.Warehouses.Add(model);
        await logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteWarehouse(Guid id)
    {
        if (id == Guid.Empty) return BadRequest();

        var warehouse = await db.Warehouses.FindAsync(id);
        if (warehouse == null) return NotFound("رکوردی یافت نشد!");

        var user = await userManager.GetUserAsync(HttpContext.User);

        warehouse.IsDeleted = true;
        warehouse.UserModifyID = user.Id;
        warehouse.UserModify = user.FullName;
        warehouse.ModifyDate = DateTime.Now;
        warehouse.ModifyDateTxt = BaseServices.GetDateTimePersian();

        await logSystemService.LogChange(warehouse, null, LogActionEnum.حذف, user);
        await db.SaveChangesAsync();
        return Ok();
    }


    public async Task<IActionResult> GetWarehouseProduct(Guid id)
    {
        try
        {
            var item = await db.WarehousesProducts.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (item == null) return NotFound("رکوردی یافت نشد!");

            return Ok(item);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetWarehouseProductsDt(Guid warehouseId)
    {
        var checks = await db.WarehousesProducts
            .AsNoTracking()
            .Where(f => f.WarehouseId == warehouseId)
            .OrderByDescending(f => f.CreateDate)
            .ToListAsync();
        var model = new {
            data = checks,
        };
        return Ok(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddWarehouseProduct([FromBody] WarehouseProduct model)
    {
        if (model == null) return BadRequest();

        var user = await userManager.GetUserAsync(HttpContext.User);
        model.UnitTxt = model.Unit.ToString().Replace("_", " ");

        if (model.Id == Guid.Empty)
        {
            model.UserCreator = user.FullName;
            model.UserCreatorID = user.Id;

            db.WarehousesProducts.Add(model);
            await logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
        } else
        {
            var oldItem = await db.WarehousesProducts.AsNoTracking().FirstOrDefaultAsync(f => f.Id == model.Id);
            model.ModifyDate = DateTime.Now;
            model.ModifyDateTxt = BaseServices.GetDateTimePersian();
            model.UserModify = user.FullName;
            model.UserModifyID = user.Id;

            db.WarehousesProducts.Update(model);
            await logSystemService.LogChange(oldItem, model, LogActionEnum.ویرایش, user);
        }


        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteWarehouseProduct(Guid id)
    {
        if (id == Guid.Empty) return BadRequest();

        var product = await db.WarehousesProducts.FindAsync(id);
        if (product == null) return NotFound("رکوردی یافت نشد!");

        var user = await userManager.GetUserAsync(HttpContext.User);

        product.IsDeleted = true;
        product.UserModifyID = user.Id;
        product.UserModify = user.FullName;
        product.ModifyDate = DateTime.Now;
        product.ModifyDateTxt = BaseServices.GetDateTimePersian();


        await logSystemService.LogChange(product, null, LogActionEnum.حذف, user);
        await db.SaveChangesAsync();
        return Ok();
    }
}