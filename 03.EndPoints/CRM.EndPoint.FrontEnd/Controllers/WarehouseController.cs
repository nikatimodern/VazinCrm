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
                WarehouseTransactionsCount = c.WarehouseTransactions.Count
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

        if (model.Id == Guid.Empty)
        {
            model.UserCreatorID = user.Id;
            model.UserCreator = user.FullName;

            db.Warehouses.Add(model);
            await logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
        } else
        {
            var oldItem = await db.Warehouses.AsNoTracking().FirstOrDefaultAsync(f => f.Id == model.Id);
            model.ModifyDate = DateTime.Now;
            model.ModifyDateTxt = BaseServices.GetDateTimePersian();
            model.UserModify = user.FullName;
            model.UserModifyID = user.Id;

            db.Warehouses.Update(model);
            await logSystemService.LogChange(oldItem, model, LogActionEnum.ویرایش, user);
        }

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


    public async Task<IActionResult> GetWarehouse(Guid id)
    {
        try
        {
            var item = await db.Warehouses.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (item == null) return NotFound("رکوردی یافت نشد!");

            return Ok(item);
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }


    public async Task<IActionResult> GetWarehouseTransaction(Guid id)
    {
        try
        {
            var item = await db.WarehouseTransactions.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (item == null) return NotFound("رکوردی یافت نشد!");

            return Ok(item);
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetWarehouseTransactionsDt(Guid warehouseId)
    {
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
        var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
        var searchValue = Request.Form["search[value]"].FirstOrDefault();
        var sortColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
        var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault();
        var sortColumn = Request.Form[$"columns[{sortColumnIndex}][data]"].FirstOrDefault();
//        var model = JsonConvert.DeserializeObject<FilterVModel>(Request.Form["model"].FirstOrDefault());


        var query = db.WarehouseTransactions
            .AsNoTracking()
            .Where(f => f.WarehouseId == warehouseId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchValue))
        {
            var serVal = searchValue.Trim();
            query = query.Where(f =>
                f.ProductTxt.Contains(serVal)
            );
        }

        if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortDirection))
        {
            var isAsc = sortDirection == "asc";

            query = sortColumn switch {
                "productTxt" => isAsc ? query.OrderBy(c => c.ProductTxt) : query.OrderByDescending(c => c.ProductTxt),
                "weight" => isAsc ? query.OrderBy(c => c.Weight) : query.OrderByDescending(c => c.Weight),
                "transactionType" => isAsc ? query.OrderBy(c => c.TransactionType) : query.OrderByDescending(c => c.TransactionType),
                _ => isAsc ? query.OrderBy(c => c.CreateDate) : query.OrderByDescending(c => c.CreateDate),
            };
        } else
        {
            query = query.OrderByDescending(c => c.CreateDate);
        }

        var recordsTotal = await query.CountAsync();

        if (length != -1)
        {
            query = query.Skip(start).Take(length);
        }

        var data = await query.ToListAsync();

        return Json(new {
            draw,
            recordsTotal,
            recordsFiltered = recordsTotal,
            data,
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddWarehouseTransaction([FromBody] WarehouseTransaction model)
    {
        if (model == null) return BadRequest();

        var user = await userManager.GetUserAsync(HttpContext.User);
        model.UnitTxt = model.Unit.ToString().Replace("_", " ");

        if (model.Id == Guid.Empty)
        {
            model.UserCreator = user.FullName;
            model.UserCreatorID = user.Id;

            db.WarehouseTransactions.Add(model);
            await logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
        } else
        {
            var oldItem = await db.WarehouseTransactions.AsNoTracking().FirstOrDefaultAsync(f => f.Id == model.Id);
            model.ModifyDate = DateTime.Now;
            model.ModifyDateTxt = BaseServices.GetDateTimePersian();
            model.UserModify = user.FullName;
            model.UserModifyID = user.Id;

            db.WarehouseTransactions.Update(model);
            await logSystemService.LogChange(oldItem, model, LogActionEnum.ویرایش, user);
        }


        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteWarehouseTransaction(Guid id)
    {
        if (id == Guid.Empty) return BadRequest();

        var product = await db.WarehouseTransactions.FindAsync(id);
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