using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.EndPoint.FrontEnd.Models.Warehouse;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.Extensions;
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

    private async Task<WarehousePageVModel> GetAllWarehouses()
    {
        var isManager = User.IsAdmin() || User.IsManagingDirector() || User.IsTaminAdmin();

        var query = db.Warehouses.AsNoTracking();

        if (!isManager)
        {
            query = query.Where(x => x.WarehouseUsers.Any(u => u.UserId == User.GetUserId()));
        }

        var warehouses = await query
            .Select(x => new WarehouseDataVModel {
                Warehouse = x,
                WarehouseProductsCount = x.WarehouseProducts.Count,
                UserIds = x.WarehouseUsers.Select(x => x.UserId).ToList(),
            })
            .ToListAsync();

        var userIds = warehouses.SelectMany(x => x.UserIds).Distinct().ToList();

        var userMap = await userManager.Users
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName);

        warehouses.ForEach(x =>
            x.UserNames = x.UserIds
                .Where(userMap.ContainsKey)
                .Select(id => userMap[id])
                .ToList());

        return new WarehousePageVModel {
            Warehouses = warehouses,
            IsManager = isManager
        };
    }


    public async Task<IActionResult> GetAllWarehousesPartial()
    {
        var warehouses = await GetAllWarehouses();
        return PartialView(warehouses);
    }

    public async Task<IActionResult> GetUsers()
    {
        var warehouses = await GetAllWarehouses();
        return PartialView(warehouses);
    }

    [HttpPost]
    public async Task<IActionResult> AddWarehouse([FromBody] WarehouseVModel model)
    {
        if (model == null) return BadRequest();

        var user = await userManager.GetUserAsync(HttpContext.User);

        Warehouse warehouse;
        if (model.Id == Guid.Empty)
        {
            warehouse = new Warehouse {
                Title = model.Title,
                UserCreator = user.FullName,
                UserCreatorID = user.Id,
                WarehouseUsers = model.UserIds?.ConvertAll(userId => new WarehouseUser { UserId = userId })
            };

            db.Warehouses.Add(warehouse);
            await logSystemService.LogChange(warehouse, null, LogActionEnum.ثبت, user);
        } else
        {
            warehouse = await db.Warehouses.Include(w => w.WarehouseUsers).FirstOrDefaultAsync(x => x.Id == model.Id);
            if (warehouse == null) return NotFound();

            warehouse.Title = model.Title;
            warehouse.ModifyDate = DateTime.Now;
            warehouse.ModifyDateTxt = BaseServices.GetDateTimePersian();
            warehouse.UserModify = user.FullName;
            warehouse.UserModifyID = user.Id;

            warehouse.WarehouseUsers = model.UserIds?.ConvertAll(userId => new WarehouseUser { WarehouseId = warehouse.Id, UserId = userId });

            var oldWarehouse = (Warehouse)db.Entry(warehouse).OriginalValues.ToObject();
            await logSystemService.LogChange(oldWarehouse, warehouse, LogActionEnum.ویرایش, user, ["WarehouseUsers"]);
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
            var item = await db.Warehouses.AsNoTracking()
                .Include(f => f.WarehouseUsers)
                .Select(f => new WarehouseVModel {
                    Id = f.Id,
                    Title = f.Title,
                    UserIds = f.WarehouseUsers.Select(f => f.UserId).ToList()
                })
                .FirstOrDefaultAsync(f => f.Id == id);

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
            if (item == null) return NotFound();

            return Ok(item);
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetWarehouseProductsDt(Guid warehouseId)
    {
        if (warehouseId == Guid.Empty) return BadRequest();
        try
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            var sortColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
            var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var sortColumn = Request.Form[$"columns[{sortColumnIndex}][data]"].FirstOrDefault();
//        var model = JsonConvert.DeserializeObject<FilterVModel>(Request.Form["model"].FirstOrDefault());

            var query = db.WarehouseProducts
                .AsNoTracking()
                .Where(f => f.WarehouseId == warehouseId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchValue))
            {
                var serVal = searchValue.Trim();
                query = query.Where(f =>
                    f.ProductDescription.Contains(serVal) ||
                    f.ProductSmallName.Contains(serVal)
                );
            }

            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortDirection))
            {
                var isAsc = sortDirection == "asc";

                query = sortColumn switch {
                    "productSmallName" => isAsc ? query.OrderBy(c => c.ProductSmallName) : query.OrderByDescending(c => c.ProductSmallName),
                    "productDescription" => isAsc ? query.OrderBy(c => c.ProductDescription) : query.OrderByDescending(c => c.ProductDescription),
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

            var data = await query
                .Select(x => new {
                    x.Id,
                    x.ProductCode,
                    x.ProductSmallName,
                    x.ProductDescription,
                    x.Unit,
                    x.UnitTxt,
                    x.UserCreator,
                    Stock = x.Transactions.Sum(t => t.TransactionType == WarehouseTransactionTypeEnum.ورود ? t.Weight : -t.Weight)
                })
                .ToListAsync();

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data,
            });
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetWarehouseTransactionsDt(Guid productId)
    {
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
        var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
        var searchValue = Request.Form["search[value]"].FirstOrDefault();
        var sortColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
        var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault();
        var sortColumn = Request.Form[$"columns[{sortColumnIndex}][data]"].FirstOrDefault();

        var query = db.WarehouseTransactions
            .AsNoTracking()
            .Where(f => f.WarehouseProductId == productId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchValue))
        {
            var serVal = searchValue.Trim();
            /*query = query.Where(f =>
                f.ProductTxt.Contains(serVal)
            );*/
        }

        if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortDirection))
        {
            var isAsc = sortDirection == "asc";

            query = sortColumn switch {
                "weight" => isAsc ? query.OrderBy(c => c.Weight) : query.OrderByDescending(c => c.Weight),
                "transactionType" => isAsc ? query.OrderBy(c => c.TransactionType) : query.OrderByDescending(c => c.TransactionType),
                "userCreator" => isAsc ? query.OrderBy(c => c.UserCreator) : query.OrderByDescending(c => c.UserCreator),
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
    public async Task<IActionResult> AddWarehouseTransaction([FromBody] WarehouseTransactionVModel model)
    {
        if (model == null) return BadRequest();


        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Unauthorized();

        if (model.Weight <= 0)
            return BadRequest("مقدار باید بیشتر از صفر باشد.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            WarehouseProduct warehouseProduct;
            if (!model.WarehouseProductId.HasValue) // محصول جدید
            {
                var exists = await db.WarehouseProducts.AnyAsync(x =>
                    x.WarehouseId == model.WarehouseId &&
                    x.ProductCode == model.ProductCode &&
                    x.ProductDescription == model.ProductDescription);

                if (exists)
                    return Conflict("این محصول قبلاً در این انبار ثبت شده است.");

                warehouseProduct = new WarehouseProduct {
                    WarehouseId = model.WarehouseId,
                    ProductCode = model.ProductCode,
                    ProductSmallName = model.ProductSmallName,
                    ProductDescription = model.ProductDescription,
                    Unit = model.Unit,
                    UnitTxt = model.Unit.ToString().Replace("_", " "),
                    UserCreator = user.FullName,
                    UserCreatorID = user.Id
                };

                db.WarehouseProducts.Add(warehouseProduct);
                await logSystemService.LogChange(warehouseProduct, null, LogActionEnum.ثبت, user);
            } else
            {
                warehouseProduct = await db.WarehouseProducts.FirstOrDefaultAsync(x => x.Id == model.WarehouseProductId.Value);

                if (warehouseProduct == null)
                    return NotFound("محصول مورد نظر یافت نشد.");

                // خروج از انبار
                if (model.TransactionType == WarehouseTransactionTypeEnum.خروج)
                {
                    var currentStock = await db.WarehouseTransactions.GetStockAsync(warehouseProduct.Id);

                    if (model.Weight > currentStock)
                        return BadRequest($"موجودی کافی نیست. موجودی فعلی: {currentStock} {warehouseProduct.UnitTxt}");
                }
            }

            var warehouseTransaction = new WarehouseTransaction {
                WarehouseProductId = warehouseProduct.Id,
                Weight = model.Weight,
                TransactionType = model.TransactionType,
                UserCreator = user.FullName,
                UserCreatorID = user.Id
            };

            db.WarehouseTransactions.Add(warehouseTransaction);
            await logSystemService.LogChange(warehouseTransaction, null, LogActionEnum.ثبت, user);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateWarehouseTransaction([FromBody] WarehouseTransaction model)
    {
        if (model == null) return BadRequest();
        if (model.Weight <= 0) return BadRequest("مقدار باید بیشتر از صفر باشد.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var data = await db.WarehouseTransactions.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (data == null) return NotFound();

            var user = await userManager.GetUserAsync(HttpContext.User);

            // موجودی فعلی بدون این تراکنش
            var stockWithoutCurrent = await db.WarehouseTransactions.GetStockWithoutTransactionAsync(data.WarehouseProductId, model.Id);

            var newStock = stockWithoutCurrent + (model.TransactionType == WarehouseTransactionTypeEnum.ورود ? model.Weight : -model.Weight);

            if (newStock < 0) return BadRequest("موجودی کافی نیست.");


            data.Weight = model.Weight;

            data.ModifyDate = DateTime.Now;
            data.ModifyDateTxt = BaseServices.GetDateTimePersian();
            data.UserModify = user.FullName;
            data.UserModifyID = user.Id;

            var oldData = (WarehouseTransaction)db.Entry(data).OriginalValues.ToObject();
            await logSystemService.LogChange(oldData, data, LogActionEnum.ویرایش, user);


            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(new { stock = newStock });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteWarehouseTransaction(Guid id)
    {
        if (id == Guid.Empty) return BadRequest();

        var warehouseTransaction = await db.WarehouseTransactions.FindAsync(id);
        if (warehouseTransaction == null) return NotFound("رکوردی یافت نشد!");

        var user = await userManager.GetUserAsync(HttpContext.User);

        warehouseTransaction.IsDeleted = true;

        warehouseTransaction.UserModifyID = user.Id;
        warehouseTransaction.UserModify = user.FullName;
        warehouseTransaction.ModifyDate = DateTime.Now;
        warehouseTransaction.ModifyDateTxt = BaseServices.GetDateTimePersian();

        await logSystemService.LogChange(warehouseTransaction, null, LogActionEnum.حذف, user);
        await db.SaveChangesAsync();

        var stock = await db.WarehouseTransactions.AsNoTracking().GetStockAsync(warehouseTransaction.WarehouseProductId);

        return Ok(new { stock });
    }
}