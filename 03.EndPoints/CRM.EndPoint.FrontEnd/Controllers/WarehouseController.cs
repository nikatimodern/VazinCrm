using CRM.Core.Domain.Supplies.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.Core.Domain.Warehouses.Repositories;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.Models.Supply;
using CRM.EndPoint.FrontEnd.Models.Warehouse;
using CRM.EndPoint.FrontEnd.Services.Extensions;
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
public class WarehouseController(CRMContextDb db, UserManager<AppUser> userManager, LogSystemService logSystemService, IViewRenderService vrs, IWarehouseRepository warehouseRepository)
    : Controller {
    private bool CanManageWarehouses() =>
        User.IsAdmin() || User.IsManagingDirector() || User.IsTaminAdmin() || User.IsSalesAdmin();

    private const string SystemWarehouseMessage = "عملیات دستی انبار اصلی فقط برای مدیر ارشد مجاز است.";

    private Task<bool> IsManualWriteBlockedAsync(Guid? warehouseId = null, Guid[] productIds = null,
        Guid? voucherId = null, Guid? transactionId = null) =>
        User.IsAdmin() ? Task.FromResult(false) :
            warehouseRepository.IsManualWriteBlockedAsync(warehouseId, productIds ?? [],
                voucherId, transactionId, HttpContext.RequestAborted);

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
        if (!CanManageWarehouses()) return Forbid();
        if (model.IsMain && !User.IsAdmin()) return Forbid();

        Warehouse warehouse = null;
        if (model.Id != Guid.Empty) {
            warehouse = await warehouseRepository.FindAsync(model.Id, HttpContext.RequestAborted);
            if (warehouse == null) return NotFound();
            if (!warehouse.CanManage(User.IsAdmin())) return Forbid();
        }

        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Unauthorized();

        if (model.Id == Guid.Empty)
        {
            warehouse = new Warehouse {
                Title = model.Title,
                IsMain = model.IsMain,
                UserCreator = user.FullName,
                UserCreatorID = user.Id,
                WarehouseUsers = model.UserIds?.ConvertAll(userId => new WarehouseUser { UserId = userId })
            };

            warehouseRepository.Add(warehouse);
            await logSystemService.LogChange(warehouse, null, LogActionEnum.ثبت, user);
        } else
        {
            warehouse.IsMain = model.IsMain;
            warehouse.Title = model.Title;
            warehouse.ModifyDate = DateTime.Now;
            warehouse.ModifyDateTxt = BaseServices.GetDateTimePersian();
            warehouse.UserModify = user.FullName;
            warehouse.UserModifyID = user.Id;

            warehouse.WarehouseUsers = model.UserIds?.ConvertAll(userId => new WarehouseUser { WarehouseId = warehouse.Id, UserId = userId });

            var oldWarehouse = await db.Warehouses.AsNoTracking().Include(w => w.WarehouseUsers).FirstOrDefaultAsync(x => x.Id == model.Id);
            await logSystemService.LogChange(oldWarehouse, warehouse, LogActionEnum.ویرایش, user, ["WarehouseUsers"]);
        }

        await warehouseRepository.SaveChangesAsync(HttpContext.RequestAborted);
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
                    IsMain = f.IsMain,
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

        if (!CanManageWarehouses()) return Forbid();
        var warehouse = await warehouseRepository.FindAsync(id, HttpContext.RequestAborted);
        if (warehouse == null) return NotFound("رکوردی یافت نشد!");

        if (!warehouse.CanManage(User.IsAdmin())) return Forbid();

        var user = await userManager.GetUserAsync(HttpContext.User);

        warehouse.IsDeleted = true;
        warehouse.UserModifyID = user.Id;
        warehouse.UserModify = user.FullName;
        warehouse.ModifyDate = DateTime.Now;
        warehouse.ModifyDateTxt = BaseServices.GetDateTimePersian();

        await logSystemService.LogChange(warehouse, null, LogActionEnum.حذف, user);
        await warehouseRepository.SaveChangesAsync(HttpContext.RequestAborted);
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
            var data = await db.WarehouseProducts
                .AsNoTracking()
                .Where(f => model.FilterIds.Contains(f.Id))
                .Select(f => new {
                    WarehouseProductId = f.Id,
                    f.ProductDescription,
                    f.ProductSmallName,
                    f.UnitTxt,
                    WStock = f.Transactions.Sum(t => t.TransactionType == WarehouseTransactionTypeEnum.ورود ? t.Weight : -t.Weight),
                    InCirculation = f.WarehouseProductVouchers.Where(v => v.WarehouseVoucher.Status != WarehouseVoucherStatusEnum.تحویل_شده)
                        .Sum(v => v.Weight)
                })
                .Select(x => new {
                    x.WarehouseProductId,
                    x.ProductDescription,
                    x.ProductSmallName,
                    x.UnitTxt,
                    Stock = x.WStock - x.InCirculation
                })
                .ToListAsync();
            return Ok(data);
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
                    WarehouseProductId = x.Id,
                    x.ProductDescription,
                    x.ProductSmallName,
                    x.UnitTxt,
                    x.Id,
                    x.ProductCode,
                    x.Unit,
                    x.UserCreator,
                    WStock = x.Transactions.Sum(t => t.TransactionType == WarehouseTransactionTypeEnum.ورود ? t.Weight : -t.Weight),
                    InCirculation = x.WarehouseProductVouchers.Where(v => v.WarehouseVoucher.Status != WarehouseVoucherStatusEnum.تحویل_شده)
                        .Sum(v => v.Weight)
                })
                .Select(x => new {
                    x.Id,
                    x.ProductCode,
                    x.ProductSmallName,
                    x.ProductDescription,
                    x.Unit,
                    x.UnitTxt,
                    x.UserCreator,
                    Stock = x.WStock - x.InCirculation
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
        if (await IsManualWriteBlockedAsync(model.WarehouseId, [model.Id]))
            return BadRequest(SystemWarehouseMessage);

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
            if (oldData == null) return NotFound();
            if (oldData?.SupplyFactorItemId != null || oldData?.FactorItemId != null)
                return BadRequest("محصول مرتبط با تخصیص فقط از فاکتور مربوط قابل ویرایش است.");
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
        if (await IsManualWriteBlockedAsync(productIds: [id]))
            return BadRequest(SystemWarehouseMessage);

        if (id == Guid.Empty) return BadRequest();

        var warehouseProduct = await db.WarehouseProducts.FindAsync(id);
        if (warehouseProduct == null) return NotFound("رکوردی یافت نشد!");
        if (warehouseProduct.SupplyFactorItemId != null || warehouseProduct.FactorItemId != null)
            return BadRequest("محصول مرتبط با تخصیص قابل حذف مستقیم نیست.");


        var hasVoucher = await db.WarehouseProductVouchers.AnyAsync(f => f.WarehouseProductId == id);
        if (hasVoucher) return BadRequest("این محصول دارای حواله انبار است و امکان حذف آن وجود ندارد.");

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

    private async Task AddTransaction(WarehouseTransactionVModel model, AppUser user)
    {
        if (await IsManualWriteBlockedAsync(model.WarehouseId,
                model.WarehouseProductId.HasValue ? [model.WarehouseProductId.Value] : [], model.WarehouseVoucherId))
            throw new InvalidOperationException(SystemWarehouseMessage);

        if (model.Weight <= 0)
            throw new ArgumentException("مقدار باید بیشتر از صفر باشد.");

        var warehouseProduct = await GetOrCreateWarehouseProduct(model, user);
        if (!User.IsAdmin() && warehouseProduct.WarehouseId == SupplyAllocationRules.CirculationId)
            throw new InvalidOperationException("گردش این انبار به‌صورت خودکار از فاکتور تامین ثبت می‌شود.");

/*        if (model.TransactionType == WarehouseTransactionTypeEnum.خروج)
            await ValidateStock(warehouseProduct, model.Weight, model.Id);*/

        var warehouseTransaction = new WarehouseTransaction {
            WarehouseProductId = warehouseProduct.Id,
            WarehouseVoucherId = model.WarehouseVoucherId,
            Weight = model.Weight,
            TransactionType = model.TransactionType,
            UserCreator = user.FullName,
            UserCreatorID = user.Id
        };

        db.WarehouseTransactions.Add(warehouseTransaction);

        await logSystemService.LogChange(warehouseTransaction, null, LogActionEnum.ثبت, user);
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

    private async Task ValidateStock(WarehouseProduct warehouseProduct, decimal weight, Guid? excludedVoucherId = null)
    {
        var availableStock = await db.GetWarehouseProductAvailableStockAsync(warehouseProduct.Id, excludedVoucherId: excludedVoucherId);

        if (weight > availableStock)
        {
            throw new InvalidOperationException(
                $"موجودی ({warehouseProduct.ProductSmallName} {warehouseProduct.ProductDescription}) کافی نیست. \n" +
                $"موجودی فعلی: {availableStock} {warehouseProduct.UnitTxt}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddWarehouseTransaction([FromBody] WarehouseTransactionVModel model)
    {
        if (model == null) return BadRequest();
        if (await IsManualWriteBlockedAsync(model.WarehouseId,
                model.WarehouseProductId.HasValue ? [model.WarehouseProductId.Value] : [],
                model.WarehouseVoucherId))
            return BadRequest(SystemWarehouseMessage);

        if (model == null) return BadRequest();

        var user = await userManager.GetUserAsync(HttpContext.User);
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            await AddTransaction(model, user);
            await db.SaveChangesAsync();
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
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateWarehouseTransaction([FromBody] WarehouseTransaction model)
    {
        if (model == null) return BadRequest();
        if (await IsManualWriteBlockedAsync(productIds: [model.WarehouseProductId],
                voucherId: model.WarehouseVoucherId, transactionId: model.Id))
            return BadRequest(SystemWarehouseMessage);

        if (model == null) return BadRequest();
        if (model.Weight <= 0) return BadRequest("مقدار باید بیشتر از صفر باشد.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var data = await db.WarehouseTransactions.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (data == null) return NotFound();
            if (data.SupplyFactorItemId != null || data.FactorItemId != null)
                return BadRequest("تراکنش خودکار تخصیص فقط از فاکتور مربوط قابل اصلاح است.");

            var user = await userManager.GetUserAsync(HttpContext.User);

            // موجودی فعلی بدون این تراکنش

            var availableStockWithoutCurrent =
                await db.GetWarehouseProductAvailableStockAsync(data.WarehouseProductId, excludedTransactionId: data.Id);
            var newStock = availableStockWithoutCurrent + (model.TransactionType == WarehouseTransactionTypeEnum.ورود ? model.Weight : -model.Weight);

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
        if (await IsManualWriteBlockedAsync(transactionId: id))
            return BadRequest(SystemWarehouseMessage);

        if (id == Guid.Empty) return BadRequest();

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var warehouseTransaction = await db.WarehouseTransactions.FirstOrDefaultAsync(x => x.Id == id);

            if (warehouseTransaction == null)
                return NotFound("رکوردی یافت نشد!");
            if (warehouseTransaction.SupplyFactorItemId != null || warehouseTransaction.FactorItemId != null)
                return BadRequest("تراکنش خودکار تخصیص قابل حذف مستقیم نیست.");

            var availableStock = await db.GetWarehouseProductAvailableStockAsync(warehouseTransaction.WarehouseProductId,
                excludedTransactionId: warehouseTransaction.Id);

            if (availableStock < 0)
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

            return Ok(new { stock = availableStock });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }


    /*--------------------------------- warehouseVoucher ----------------------------------*/

    public async Task<IActionResult> Voucher(Guid? warehouseId = null)
    {
        var data = await GetWarehouseVouchersAsync(model: new WarehouseVoucherListVModel(), warehouseId: warehouseId);
        return View(data);
    }

    [HttpPost]
    public async Task<IActionResult> GetWarehouseVouchers([FromBody] WarehouseVoucherListVModel model, [FromQuery] Guid? warehouseId = null)
    {
        var data = await GetWarehouseVouchersAsync(model, warehouseId: warehouseId);

        var html = await vrs.RenderToStringAsync("_WarehouseVouchers", data);
        return Ok(new { data.DisplayFrom, data.DisplayTo, data.TotalCount, html });
    }

    [HttpGet]
    public async Task<IActionResult> GetLastWarehouseVouchers(Guid warehouseId)
    {
        var model = new WarehouseVoucherListVModel {
            Page = 1,
            PageSize = 6,
        };

        var data = await GetWarehouseVouchersAsync(model, warehouseId: warehouseId);
        return PartialView("_WarehouseVouchersGrid", data.Items);
    }


    private async Task<WarehouseVoucherListVModel> GetWarehouseVouchersAsync(WarehouseVoucherListVModel model, Guid? warehouseId = null)
    {
        var query = db.WarehouseVouchers
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.WarehouseProductVouchers)
            .ThenInclude(x => x.WarehouseProduct)
            .AsQueryable();

        var isManager = User.IsAdmin() || User.IsManagingDirector() || User.IsTaminAdmin() || User.IsSalesAdmin();

        if (!isManager)
        {
            var userId = User.GetUserId();

            query = query.Where(voucher => // حواله های مربوط به انبار خودش یا حواله هایی که خودش ثبت کرده
                voucher.Warehouse.WarehouseUsers.Any(wu => wu.UserId == userId) ||
                voucher.UserCreatorID == userId
            );
        }

        var warehouses = await query
            .Select(x => new WarehouseVModel {
                Id = x.Warehouse.Id,
                Title = x.Warehouse.Title
            })
            .Distinct()
            .OrderBy(x => x.Title)
            .ToListAsync();


        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId);
        }

        if (model.Status.HasValue)
        {
            query = query.Where(x => x.Status == model.Status);
        }

        if (!string.IsNullOrWhiteSpace(model.Search))
        {
            var search = model.Search.Trim();

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

        var page = Math.Max(model.Page, 1);

        var items = await query
            .OrderByDescending(x => x.CreateDate)
            .Skip((page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToListAsync();


        return new WarehouseVoucherListVModel {
            Items = items,
            Page = page,
            PageSize = model.PageSize,
            TotalCount = totalCount,
            Warehouses = warehouses,
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetWarehouseVoucher(Guid id)
    {
        if (id == Guid.Empty) return BadRequest();

        var warehouseProductVouchers = await db.WarehouseProductVouchers
            .AsNoTracking()
            .Where(x => x.WarehouseVoucherId == id)
            .Select(v => new {
                v.WarehouseProductId,
                v.Weight,
                v.WarehouseProduct.ProductSmallName,
                v.WarehouseProduct.ProductDescription,
                v.WarehouseProduct.UnitTxt,
                WStock = v.WarehouseProduct.Transactions.Sum(t => t.TransactionType == WarehouseTransactionTypeEnum.ورود ? t.Weight : -t.Weight),
                InCirculation = v.WarehouseProduct.WarehouseProductVouchers
                    .Where(x => x.WarehouseVoucher.Status != WarehouseVoucherStatusEnum.تحویل_شده && x.WarehouseVoucherId != id)
                    .Sum(x => x.Weight)
            })
            .Select(v => new {
                v.WarehouseProductId,
                v.Weight,
                v.ProductSmallName,
                v.ProductDescription,
                v.UnitTxt,
                Stock = v.WStock - v.InCirculation
            })
            .ToListAsync();

        return Ok(warehouseProductVouchers);
    }

    public async Task<IActionResult> VoucherDetail(Guid id)
    {
        if (id == Guid.Empty) return BadRequest();

        var voucher = await db.WarehouseVouchers
            .Include(x => x.Warehouse)
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
        if (await IsManualWriteBlockedAsync(model.WarehouseId,
                model.WarehouseProductVouchers?.Select(p => p.WarehouseProductId).Distinct().ToArray() ?? []))
            return BadRequest(SystemWarehouseMessage);

        if (model?.WarehouseProductVouchers == null || model.WarehouseProductVouchers.Count == 0)
            return BadRequest("حداقل یک محصول باید در حواله وجود داشته باشد.");

        if (model.WarehouseProductVouchers.Any(x => x.Weight <= 0))
            return BadRequest("مقدار باید بیشتر از صفر باشد.");

        var user = await userManager.GetUserAsync(HttpContext.User);

        var productIds = model.WarehouseProductVouchers
            .Select(x => x.WarehouseProductId)
            .Distinct()
            .ToList();

        var warehouseProducts = (await db.WarehouseProducts.Where(x => productIds.Contains(x.Id)).ToListAsync()).ToDictionary(x => x.Id);

        if (warehouseProducts.Count == 0)
            return NotFound("محصول مورد نظر یافت نشد.");

        var warehouseIds = warehouseProducts.Values.Select(x => x.WarehouseId).Distinct().ToList();
        if (warehouseIds.Count != 1)
            return BadRequest("محصولات حواله باید متعلق به یک انبار باشند.");

        var warehouseId = warehouseIds[0];
        if (!User.IsAdmin() && warehouseId == SupplyAllocationRules.CirculationId)
            return BadRequest("خروج انبار گردشی از طریق تخصیص فاکتور تامین ثبت می‌شود.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in model.WarehouseProductVouchers)
            {
                if (!warehouseProducts.TryGetValue(item.WarehouseProductId, out var warehouseProduct))
                    return NotFound("محصول مورد نظر یافت نشد.");

                await ValidateStock(warehouseProduct, item.Weight);
            }

            model.WarehouseId = warehouseId;
            model.UserCreator = user.FullName;
            model.UserCreatorID = user.Id;
            model.Status = WarehouseVoucherStatusEnum.در_انتظار_تایید_تامین;

            db.WarehouseVouchers.AddRange(model);
            await db.SaveChangesAsync();
            await logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            return Conflict(ex.Message);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> EditWarehouseVoucher([FromBody] WarehouseVoucher model)
    {
        if (model == null) return BadRequest();
        if (await IsManualWriteBlockedAsync(model.WarehouseId,
                model.WarehouseProductVouchers?.Select(p => p.WarehouseProductId).Distinct().ToArray() ?? [], model.Id))
            return BadRequest(SystemWarehouseMessage);

        if (model == null || model.Id == Guid.Empty)
            return BadRequest();

        if (model.WarehouseProductVouchers == null || model.WarehouseProductVouchers.Count == 0)
            return BadRequest("حداقل یک محصول باید در حواله وجود داشته باشد.");

        if (model.WarehouseProductVouchers.Any(x => x.Weight <= 0))
            return BadRequest("مقدار باید بیشتر از صفر باشد.");

        var user = await userManager.GetUserAsync(HttpContext.User);

        if (user == null)
            return Unauthorized();

        var voucher = await db.WarehouseVouchers
            .Include(x => x.WarehouseProductVouchers)
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == model.Id);

        if (voucher == null)
            return NotFound("حواله مورد نظر یافت نشد.");

        if (voucher.Status == WarehouseVoucherStatusEnum.تحویل_شده)
            return BadRequest("این حواله در وضعیت قابل ویرایش نیست.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in model.WarehouseProductVouchers)
            {
                var warehouseProduct = await db.WarehouseProducts.FirstOrDefaultAsync(x => x.Id == item.WarehouseProductId);

                if (warehouseProduct == null)
                    return NotFound("محصول مورد نظر یافت نشد.");

                if (!User.IsAdmin() && warehouseProduct.WarehouseId == SupplyAllocationRules.CirculationId)
                    return BadRequest("خروج انبار گردشی از طریق تخصیص فاکتور تامین ثبت می‌شود.");
                await ValidateStock(warehouseProduct, item.Weight, voucher.Id);
            }

            voucher.WarehouseProductVouchers = model.WarehouseProductVouchers;

            voucher.UserModifyID = user.Id;
            voucher.UserModify = user.FullName;
            voucher.ModifyDate = DateTime.Now;
            voucher.ModifyDateTxt = BaseServices.GetDateTimePersian();

            var oldVoucher = (WarehouseVoucher)db.Entry(voucher).OriginalValues.ToObject();
            await logSystemService.LogChange(oldVoucher, voucher, LogActionEnum.ویرایش, user);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
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
        if (await IsManualWriteBlockedAsync(voucherId: id))
            return BadRequest(SystemWarehouseMessage);

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
        voucher.ModifyDate = DateTime.Now;
        voucher.ModifyDateTxt = BaseServices.GetDateTimePersian();

        var oldVoucher = (WarehouseVoucher)db.Entry(voucher).OriginalValues.ToObject();
        await logSystemService.LogChange(oldVoucher, voucher, LogActionEnum.تایید_و_تغییر_وضعیت, user);
        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeliverWarehouseVoucher(Guid id)
    {
        if (await IsManualWriteBlockedAsync(voucherId: id))
            return BadRequest(SystemWarehouseMessage);

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

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            voucher.Status = WarehouseVoucherStatusEnum.تحویل_شده;

            voucher.UserModify = user.FullName;
            voucher.UserModifyID = user.Id;
            voucher.ModifyDate = DateTime.Now;
            voucher.ModifyDateTxt = BaseServices.GetDateTimePersian();

            foreach (var productVoucher in voucher.WarehouseProductVouchers ?? [])
            {
                var transactionModel = new WarehouseTransactionVModel {
                    WarehouseProductId = productVoucher.WarehouseProductId,
                    WarehouseId = productVoucher.WarehouseProduct.WarehouseId,
                    WarehouseVoucherId = voucher.Id,
                    Weight = productVoucher.Weight,
                    TransactionType = WarehouseTransactionTypeEnum.خروج
                };

                await AddTransaction(transactionModel, user);
            }

            var oldVoucher = (WarehouseVoucher)db.Entry(voucher).OriginalValues.ToObject();
            await logSystemService.LogChange(oldVoucher, voucher, LogActionEnum.تغییر_وضعیت, user);
            await db.SaveChangesAsync();
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
        catch (Exception)
        {
            return BadRequest();
        }
    }


    [HttpDelete]
    public async Task<IActionResult> DeleteWarehouseVoucher(Guid id)
    {
        if (await IsManualWriteBlockedAsync(voucherId: id))
            return BadRequest(SystemWarehouseMessage);

        if (id == Guid.Empty) return BadRequest();

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var warehouseVoucher = await db.WarehouseVouchers.Include(f => f.Transactions).FirstOrDefaultAsync(x => x.Id == id);

            if (warehouseVoucher == null)
                return NotFound("رکوردی یافت نشد!");

            if (warehouseVoucher.Status == WarehouseVoucherStatusEnum.تحویل_شده)
                return BadRequest("این حواله در وضعیت قابل حذف نیست.");

            var user = await userManager.GetUserAsync(HttpContext.User);

            warehouseVoucher.IsDeleted = true;

            warehouseVoucher.UserModifyID = user.Id;
            warehouseVoucher.UserModify = user.FullName;
            warehouseVoucher.ModifyDate = DateTime.Now;
            warehouseVoucher.ModifyDateTxt = BaseServices.GetDateTimePersian();

            if (warehouseVoucher.Status == WarehouseVoucherStatusEnum.تحویل_شده)
            {
                foreach (var item in warehouseVoucher.Transactions ?? [])
                {
                    item.IsDeleted = true;

                    item.UserModifyID = user.Id;
                    item.UserModify = user.FullName;
                    item.ModifyDate = DateTime.Now;
                    item.ModifyDateTxt = BaseServices.GetDateTimePersian();
                }
            }

            await logSystemService.LogChange(warehouseVoucher, null, LogActionEnum.حذف, user);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }
}