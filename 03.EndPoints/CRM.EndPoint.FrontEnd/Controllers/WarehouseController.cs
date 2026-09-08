using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.Models.Supply;
using CRM.EndPoint.FrontEnd.Models.Warehouse;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.Framework;
using CRM.Infrastructures.Sql;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Policy = "NotCustomerOnly")]
public class WarehouseController(CRMContextDb db, UserManager<AppUser> userManager, LogSystemService logSystemService, IViewRenderService vrs)
    : Controller {
    /*--------------------------------- warehouse ----------------------------------*/

    public async Task<IActionResult> Index()
    {
        var warehouses = await GetAllWarehouses();
        return View(warehouses);
    }

    private async Task<WarehousePageVModel> GetAllWarehouses()
    {
        var isManager = User.IsAdmin() || User.IsManagingDirector() || User.IsTaminAdmin() || User.IsSalesAdmin();

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


    /*--------------------------------- warehouseProduct ----------------------------------*/

    public async Task<IActionResult> GetWarehouseProduct(Guid id)
    {
        try
        {
            var item = await db.WarehouseProducts.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (item == null) return NotFound();

            return Ok(item);
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }


    [HttpPost]
    public async Task<IActionResult> GetWarehouseProductByFilteredIds([FromBody] GetAllBySelectedVModel model)
    {
        try
        {
            var data = await db.WarehouseProducts.AsNoTracking()
                .Where(f => model.FilterIds.Contains(f.Id))
                .Select(f => new {
                    f.Id,
                    f.ProductDescription,
                    f.ProductSmallName,
                    f.UnitTxt,
                    Stock = f.Transactions.Sum(t => t.TransactionType == WarehouseTransactionTypeEnum.ورود ? t.Weight : -t.Weight)
                })
                .ToListAsync();

            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
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
                    f.ProductSmallName.Contains(serVal) ||
                    f.UserCreator.Contains(serVal)
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
    public async Task<IActionResult> UpdateWarehouseProduct([FromBody] WarehouseProduct model)
    {
        if (model == null) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            model.UnitTxt = model.Unit.ToString().Replace("_", " ");

            model.ModifyDate = DateTime.Now;
            model.ModifyDateTxt = BaseServices.GetDateTimePersian();
            model.UserModify = user.FullName;
            model.UserModifyID = user.Id;


            var oldData = await db.WarehouseProducts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id);
            await logSystemService.LogChange(oldData, model, LogActionEnum.ویرایش, user);


            db.WarehouseProducts.Update(model);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(model);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }


    [HttpDelete]
    public async Task<IActionResult> DeleteWarehouseProduct(Guid id)
    {
        if (id == Guid.Empty) return BadRequest();

        var warehouseProduct = await db.WarehouseProducts.FindAsync(id);
        if (warehouseProduct == null) return NotFound("رکوردی یافت نشد!");

        var stock = await db.WarehouseTransactions.GetStockAsync(warehouseProduct.Id);
        if (stock > 0) return BadRequest("این محصول دارای موجودی است و امکان حذف آن وجود ندارد.");


        var user = await userManager.GetUserAsync(HttpContext.User);

        warehouseProduct.IsDeleted = true;

        warehouseProduct.UserModifyID = user.Id;
        warehouseProduct.UserModify = user.FullName;
        warehouseProduct.ModifyDate = DateTime.Now;
        warehouseProduct.ModifyDateTxt = BaseServices.GetDateTimePersian();

        await logSystemService.LogChange(warehouseProduct, null, LogActionEnum.حذف, user);
        await db.SaveChangesAsync();

        return Ok();
    }


    /*--------------------------------- warehouseTransaction ----------------------------------*/

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

    private async Task<WarehouseTransaction> AddTransaction(WarehouseTransactionVModel model, AppUser user)
    {
        if (model.Weight <= 0)
            throw new ArgumentException("مقدار باید بیشتر از صفر باشد.");

        var warehouseProduct = await GetOrCreateWarehouseProduct(model, user);

        if (model.TransactionType == WarehouseTransactionTypeEnum.خروج)
            await ValidateStock(warehouseProduct, model.Weight);

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

        return warehouseTransaction;
    }

    private async Task<WarehouseProduct> GetOrCreateWarehouseProduct(WarehouseTransactionVModel model, AppUser user)
    {
        if (model.WarehouseProductId.HasValue)
        {
            var warehouseProduct = await db.WarehouseProducts.FirstOrDefaultAsync(x => x.Id == model.WarehouseProductId.Value);

            if (warehouseProduct == null)
                throw new KeyNotFoundException("محصول مورد نظر یافت نشد.");

            return warehouseProduct;
        }

        var exists = await db.WarehouseProducts.AnyAsync(x =>
            x.WarehouseId == model.WarehouseId &&
            x.ProductCode == model.ProductCode &&
            x.ProductDescription == model.ProductDescription);

        if (exists)
            throw new InvalidOperationException("این محصول قبلاً در این انبار ثبت شده است.");

        var newWarehouseProduct = new WarehouseProduct {
            WarehouseId = model.WarehouseId,
            ProductCode = model.ProductCode,
            ProductSmallName = model.ProductSmallName,
            ProductDescription = model.ProductDescription,
            Unit = model.Unit,
            UnitTxt = model.Unit.ToString().Replace("_", " "),
            UserCreator = user.FullName,
            UserCreatorID = user.Id
        };

        db.WarehouseProducts.Add(newWarehouseProduct);

        await logSystemService.LogChange(newWarehouseProduct, null, LogActionEnum.ثبت, user);

        return newWarehouseProduct;
    }

    private async Task ValidateStock(WarehouseProduct warehouseProduct, decimal weight)
    {
        var currentStock = await db.WarehouseTransactions.GetStockAsync(warehouseProduct.Id);

        if (weight > currentStock)
        {
            throw new InvalidOperationException($"موجودی کافی نیست. موجودی فعلی: " + $"{currentStock} {warehouseProduct.UnitTxt}");
        }
    }


    [HttpPost]
    public async Task<IActionResult> AddWarehouseTransaction([FromBody] WarehouseTransactionVModel model)
    {
        if (model == null) return BadRequest();

        var user = await userManager.GetUserAsync(HttpContext.User);
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            await AddTransaction(model, user);
            await transaction.CommitAsync();
            return Ok();
        }
        catch (ArgumentException ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await transaction.RollbackAsync();
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            return Conflict(ex.Message);
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

            if (newStock < 0) return BadRequest("ویرایش این تراکنش موجب منفی شدن موجودی می‌شود و امکان‌پذیر نیست.");

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

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var warehouseTransaction = await db.WarehouseTransactions.FirstOrDefaultAsync(x => x.Id == id);

            if (warehouseTransaction == null)
                return NotFound("رکوردی یافت نشد!");

            var stockWithoutCurrent =
                await db.WarehouseTransactions.GetStockWithoutTransactionAsync(warehouseTransaction.WarehouseProductId, warehouseTransaction.Id);

            if (stockWithoutCurrent < 0)
                return BadRequest("حذف این تراکنش موجب منفی شدن موجودی می‌شود و امکان‌پذیر نیست.");

            var user = await userManager.GetUserAsync(HttpContext.User);

            warehouseTransaction.IsDeleted = true;

            warehouseTransaction.UserModifyID = user.Id;
            warehouseTransaction.UserModify = user.FullName;
            warehouseTransaction.ModifyDate = DateTime.Now;
            warehouseTransaction.ModifyDateTxt = BaseServices.GetDateTimePersian();

            await logSystemService.LogChange(warehouseTransaction, null, LogActionEnum.حذف, user);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { stock = stockWithoutCurrent });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }


    /*--------------------------------- warehouseVoucher ----------------------------------*/

    public async Task<IActionResult> Voucher()
    {
        var model = await GetWarehouseVouchersAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetWarehouseVouchers(string search, WarehouseVoucherStatusEnum? status = null, int pageSize = 10, int page = 1)
    {
        var model = await GetWarehouseVouchersAsync(search, status, page, pageSize);

        var html = await vrs.RenderToStringAsync("_WarehouseVouchersGrid", model);
        return Ok(new { model.DisplayFrom, model.DisplayTo, model.TotalCount, html });
    }


    private async Task<WarehouseVoucherListVModel> GetWarehouseVouchersAsync(string search = null, WarehouseVoucherStatusEnum? status = null,
        int page = 1,
        int pageSize = 10)
    {
        var query = db.WarehouseVouchers
            .AsNoTracking()
            .Include(x => x.WarehouseProductVouchers)
            .ThenInclude(x => x.WarehouseProduct)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            var isVoucherNumber = int.TryParse(search, out var voucherNumber);

            query = query.Where(x =>
                (isVoucherNumber && x.VoucherNumber == voucherNumber) ||
                x.UserCreator.Contains(search) ||
                x.WarehouseProductVouchers.Any(p =>
                    p.WarehouseProduct.ProductSmallName.Contains(search) ||
                    p.WarehouseProduct.ProductDescription.Contains(search)
                )
            );
        }

        var totalCount = await query.CountAsync();

        page = Math.Max(page, 1);

        var items = await query
            .OrderByDescending(x => x.CreateDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new WarehouseVoucherListVModel {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = search,
            Status = status
        };
    }

    public async Task<IActionResult> VoucherDetail(Guid id)
    {
        if (id == Guid.Empty) return BadRequest();

        var voucher = await db.WarehouseVouchers
            .Include(x => x.WarehouseProductVouchers)
            .ThenInclude(x => x.WarehouseProduct)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (voucher == null) return NotFound();

        return View(voucher);
    }

    [HttpPost]
    public async Task<IActionResult> AddWarehouseVoucher([FromBody] WarehouseVoucher model)
    {
        if (model == null) return BadRequest();
        if (model.WarehouseProductVouchers.Any(x => x.Weight <= 0)) return BadRequest("مقدار باید بیشتر از صفر باشد.");

        var user = await userManager.GetUserAsync(HttpContext.User);

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in model.WarehouseProductVouchers)
            {
                var warehouseProduct = await db.WarehouseProducts.FirstOrDefaultAsync(x => x.Id == item.WarehouseProductId);

                if (warehouseProduct == null)
                    return NotFound("محصول مورد نظر یافت نشد.");

                var currentStock = await db.WarehouseTransactions.GetStockAsync(warehouseProduct.Id);

                if (item.Weight > currentStock)
                    return BadRequest(
                        $"موجودی ({warehouseProduct.ProductSmallName} {warehouseProduct.ProductDescription}) کافی نیست. موجودی فعلی: {currentStock} {warehouseProduct.UnitTxt}");
            }

            model.UserCreator = user.FullName;
            model.UserCreatorID = user.Id;
            model.Status = WarehouseVoucherStatusEnum.در_انتظار_تایید_تامین;

            db.WarehouseVouchers.AddRange(model);
            await logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);

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
    public async Task<IActionResult> ConfirmWarehouseVoucher(Guid id)
    {
        var voucher = await db.WarehouseVouchers.FindAsync(id);
        if (voucher is null) return NotFound();

        var user = await userManager.GetUserAsync(HttpContext.User);

        if (voucher.Status != WarehouseVoucherStatusEnum.در_انتظار_تایید_تامین)
        {
            return BadRequest("این حواله در وضعیت قابل تایید قرار ندارد.");
        }

        voucher.Status = WarehouseVoucherStatusEnum.در_انتظار_تحویل;
        voucher.UserModify = user.FullName;
        voucher.UserModifyID = user.Id;

        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeliverWarehouseVoucher(Guid id)
    {
        var voucher = await db.WarehouseVouchers
            .Include(x => x.WarehouseProductVouchers)
            .ThenInclude(x => x.WarehouseProduct)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (voucher is null) return NotFound();

        var user = await userManager.GetUserAsync(HttpContext.User);

        if (voucher.Status != WarehouseVoucherStatusEnum.در_انتظار_تحویل)
        {
            return BadRequest("این حواله در وضعیت قابل تحویل قرار ندارد.");
        }

        voucher.Status = WarehouseVoucherStatusEnum.تحویل_شد;
        voucher.UserModify = user.FullName;
        voucher.UserModifyID = user.Id;

        foreach (var productVoucher in voucher.WarehouseProductVouchers ?? [])
        {
            var transactionModel = new WarehouseTransactionVModel {
                WarehouseProductId = productVoucher.WarehouseProductId,
                WarehouseId = productVoucher.WarehouseProduct.WarehouseId,
                Weight = productVoucher.Weight,
                TransactionType = WarehouseTransactionTypeEnum.خروج
            };

            await AddTransaction(transactionModel, user);
        }

        await db.SaveChangesAsync();
        return Ok();
    }
}