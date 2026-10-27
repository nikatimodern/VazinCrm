using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.Core.Domain.Warehouses.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.BaseItems.Enums;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Supply;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.AI;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.Infrastructures.Sql;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize(Policy = "NotCustomerOnly")]
    public class SupplyController : Controller {
        private readonly CRMContextDb db;
        private readonly DateTimeServices dtService;
        private readonly UserManager<AppUser> userManager;
        private readonly Service genService;
        private readonly LogSystemService _logSystemService;
        private readonly FileSaver fileSaver;
        private readonly AiService aiService;
        private readonly SupplyFactorWarehouseService supplyWarehouse;

        public SupplyController(CRMContextDb db, UserManager<AppUser> userManager, DateTimeServices dtService, Service genService,
            LogSystemService logSystemService, FileSaver fileSaver, AiService aiService, SupplyFactorWarehouseService supplyWarehouse)
        {
            this.db = db;
            this.dtService = dtService;
            this.userManager = userManager;
            this.genService = genService;
            _logSystemService = logSystemService;
            this.fileSaver = fileSaver;
            this.aiService = aiService;
            this.supplyWarehouse = supplyWarehouse;
        }

        [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
        public IActionResult SupplyFactor() // فاکتور تامین
        {
            return View();
        }

        [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
        public IActionResult Payment() // پرداختی
        {
            return View();
        }

        [Authorize(Roles = "مدیر ارشد, مدیر اجرایی")]
        public IActionResult Profit() // بهای تمام شده
        {
            return View();
        }

        [Authorize(Roles = "مدیر ارشد, مدیر اجرایی")]
        public IActionResult Demand() // مطالبات
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
        public async Task<IActionResult> GetSupplyFactorAllocations(Guid id)
        {
            var factor = await db.SupplyFactors.AsNoTracking()
                .Where(x => x.Id == id).Select(x => new { x.Id, x.SerialNumber }).FirstOrDefaultAsync();
            if (factor == null) return NotFound("فاکتور تامین یافت نشد.");
            var items = await db.SupplyFactorItems.AsNoTracking()
                .Where(x => x.SupplyFactorId == id)
                .OrderBy(x => x.CreateDate).ThenBy(x => x.Id)
                .Select(x => new {
                    x.Id, x.Product, x.ProductSmallName, x.WeightCount, x.UnitTxt,
                    Allocated = x.Allocations.Sum(a => (decimal?)a.Weight) ?? 0
                }).ToListAsync();
            // Keep historical allocations visible even if the sales invoice was soft-deleted.
            var allocations = await (
                from a in db.SupplyFactorItemAllocations.AsNoTracking()
                join si in db.SupplyFactorItems on a.SupplyFactorItemId equals si.Id
                from fi in db.FactorItems.IgnoreQueryFilters().Where(fi => fi.Id == a.FactorItemId).DefaultIfEmpty()
                from f in db.Factors.IgnoreQueryFilters().Where(f => fi != null && f.Id == fi.factorId).DefaultIfEmpty()
                from w in db.Warehouses.IgnoreQueryFilters().Where(w => w.Id == a.WarehouseId).DefaultIfEmpty()
                where si.SupplyFactorId == id && !si.IsDeleted
                orderby a.Id
                select new {
                    a.Id, a.SupplyFactorItemId, a.FactorItemId, a.WarehouseId, a.Weight,
                    a.SourceWarehouseProductId,
                    SourceWarehouseId = a.SourceWarehouseProduct != null ? (Guid?)a.SourceWarehouseProduct.WarehouseId : null,
                    SourceWarehouseTitle = a.SourceWarehouseProduct != null ? a.SourceWarehouseProduct.Warehouse.Title : null,
                    SourceWarehouseStock = a.SourceWarehouseProduct != null
                        ? a.SourceWarehouseProduct.Transactions.Where(t => !t.IsDeleted).Sum(t =>
                            t.TransactionType == WarehouseTransactionTypeEnum.ورود ? (decimal?)t.Weight : -t.Weight) ?? 0
                        : 0,
                    SourceWarehouseReserved = a.SourceWarehouseProduct != null
                        ? a.SourceWarehouseProduct.WarehouseProductVouchers
                            .Where(v => v.WarehouseVoucher.Status != WarehouseVoucherStatusEnum.تحویل_شده)
                            .Sum(v => (decimal?)v.Weight) ?? 0
                        : 0,
                    SourceWarehouseRemainder = db.SupplyFactorItemAllocations.Where(other => a.SourceWarehouseProductId != null
                                                                                             && other.SupplyFactorItemId == a.SupplyFactorItemId &&
                                                                                             other.WarehouseId == a.SourceWarehouseProduct.WarehouseId
                                                                                             && other.FactorItemId == null)
                        .Sum(other => (decimal?)other.Weight) ?? 0,
                    Customer = f != null ? f.Customer : w.Title,
                    SerialNumber = f != null ? (int?)f.SerialNumber : null,
                    Product = fi != null ? fi.Product : si.Product,
                    ProductSmallName = fi != null ? fi.ProductSmallName : si.ProductSmallName,
                    FactorItemWeightCount = fi != null ? fi.WeightCount : null,
                    FactorItemUnitTxt = fi != null ? fi.UnitTxt : si.UnitTxt,
                    WarehouseTitle = w != null ? w.Title : null,
                    FactorItemAllocated = db.SupplyFactorItemAllocations
                        .Where(other => a.FactorItemId != null && other.FactorItemId == a.FactorItemId)
                        .Sum(other => (decimal?)other.Weight) ?? 0
                }).ToListAsync();
            return Ok(new {
                factor, items, allocations,
                isAllocationComplete = items.Count > 0 && items.All(x => x.Allocated >= x.WeightCount)
            });
        }

        [HttpGet]
        [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
        public async Task<IActionResult> SearchAllocationFactorItems(string search, Guid supplyFactorItemId, Guid? allocationId = null)
        {
            var source = await db.SupplyFactorItems.AsNoTracking().Where(i => i.Id == supplyFactorItemId
                && db.SupplyFactors.Any(f => f.Id == i.SupplyFactorId)).Select(i => new { i.Unit }).FirstOrDefaultAsync();
            if (source == null) return NotFound("آیتم تامین یافت نشد.");
            var unit = (int)source.Unit;
            search = search?.Trim();
            var excludedAllocationId = allocationId ?? Guid.Empty;
            var sourceProductId = await db.SupplyFactorItemAllocations.Where(a => a.Id == excludedAllocationId
                                                                                  && a.SupplyFactorItemId == supplyFactorItemId)
                .Select(a => a.SourceWarehouseProductId).FirstOrDefaultAsync();
            var query = from i in db.FactorItems.AsNoTracking()
                join f in db.Factors.AsNoTracking() on i.factorId equals f.Id
                where i.Unit == unit
                select new {
                    i.Id, i.Product, i.ProductSmallName, i.WeightCount, i.UnitTxt,
                    f.Customer, f.SerialNumber, f.Date, i.CreateDate, i.Row,
                    Allocated = db.SupplyFactorItemAllocations.Where(a => a.FactorItemId == i.Id)
                        .Sum(a => (decimal?)a.Weight) ?? 0,
                    EditingWeight = db.SupplyFactorItemAllocations.Where(a => a.FactorItemId == i.Id
                                                                              && a.Id == excludedAllocationId &&
                                                                              a.SupplyFactorItemId == supplyFactorItemId)
                        .Sum(a => (decimal?)a.Weight) ?? 0
                };
            query = query.Where(x => !db.SupplyFactorItemAllocations.Any(a =>
                a.SupplyFactorItemId == supplyFactorItemId && a.FactorItemId == x.Id
                                                           && a.SourceWarehouseProductId == sourceProductId && a.Id != excludedAllocationId));
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.Customer.Contains(search) || x.Product.Contains(search)
                                                                     || x.ProductSmallName.Contains(search) ||
                                                                     x.SerialNumber.ToString().Contains(search));
            var items = await query.OrderByDescending(x => x.CreateDate).ThenByDescending(x => x.Id)
                .Take(201).ToListAsync();
            var warehouses = await db.Warehouses.AsNoTracking()
                .Where(w => (w.Id == SupplyFactorWarehouseService.WasteId && w.WarehouseType == WarehouseType.انبار_ضایعات)
                            || (w.Id == SupplyFactorWarehouseService.StorageId && w.WarehouseType == WarehouseType.انبار_ذخیره))
                .Select(w => new { w.Id, w.Title, w.WarehouseType }).ToListAsync();
            return Ok(new {
                warehouses, items = items.Take(200).Select(i => new {
                    i.Id, i.Product, i.ProductSmallName, i.WeightCount, i.UnitTxt, i.Customer, i.SerialNumber, i.Date,
                    i.Allocated,
                    Remaining = AllocationSalesCapacity(i.WeightCount, i.Allocated),
                    Available = AllocationSalesCapacity(i.WeightCount, i.Allocated, i.EditingWeight)
                }),
                hasMore = items.Count > 200
            });
        }

        private const string SalesAllocationRoles =
            "مدیر تامین,کارشناس تامین,مدیر ارشد,مدیر اجرایی,مدیر عامل,مدیر مالی,کارشناس مالی,مدیر فروش,کارشناس فروش";

        [HttpGet]
        [Authorize(Roles = SalesAllocationRoles)]
        public async Task<IActionResult> GetSalesFactorAllocations(Guid id)
        {
            var factor = await db.Factors.AsNoTracking().Where(f => f.Id == id)
                .Select(f => new { f.Id, f.SerialNumber }).FirstOrDefaultAsync();
            if (factor == null) return NotFound("فاکتور فروش یافت نشد.");
            var rows = await db.FactorItems.AsNoTracking().Where(i => i.factorId == id)
                .OrderBy(i => i.Row).ThenBy(i => i.CreateDate).ThenBy(i => i.Id)
                .Select(i => new {
                    i.Id, i.Product, i.ProductSmallName, i.WeightCount, i.UnitTxt,
                    Allocated = i.Allocations.Sum(a => (decimal?)a.Weight) ?? 0
                }).ToListAsync();
            var items = rows.Select(i => new {
                i.Id, i.Product, i.ProductSmallName, i.UnitTxt, i.Allocated,
                WeightCount = TryParseAllocationSalesWeight(i.WeightCount, out var weight) ? Math.Max(0, weight) : 0
            }).ToList();
            // Read the same junction rows used by the supply workspace, including historical sources.
            var allocations = await (
                from a in db.SupplyFactorItemAllocations.AsNoTracking()
                join fi in db.FactorItems on a.FactorItemId equals fi.Id
                from si in db.SupplyFactorItems.IgnoreQueryFilters().Where(i => i.Id == a.SupplyFactorItemId).DefaultIfEmpty()
                from sf in db.SupplyFactors.IgnoreQueryFilters().Where(f => si != null && f.Id == si.SupplyFactorId).DefaultIfEmpty()
                from w in db.Warehouses.IgnoreQueryFilters().Where(w => w.Id == a.WarehouseId).DefaultIfEmpty()
                where fi.factorId == id && !fi.IsDeleted
                orderby a.Id
                select new {
                    a.Id, a.SupplyFactorItemId, a.FactorItemId, a.WarehouseId, a.Weight,
                    a.SourceWarehouseProductId,
                    SourceWarehouseId = a.SourceWarehouseProduct != null ? (Guid?)a.SourceWarehouseProduct.WarehouseId : null,
                    SourceWarehouseTitle = a.SourceWarehouseProduct != null ? a.SourceWarehouseProduct.Warehouse.Title : null,
                    SourceWarehouseStock = a.SourceWarehouseProduct != null
                        ? a.SourceWarehouseProduct.Transactions.Where(t => !t.IsDeleted).Sum(t =>
                            t.TransactionType == WarehouseTransactionTypeEnum.ورود ? (decimal?)t.Weight : -t.Weight) ?? 0
                        : 0,
                    SourceWarehouseReserved = a.SourceWarehouseProduct != null
                        ? a.SourceWarehouseProduct.WarehouseProductVouchers
                            .Where(v => v.WarehouseVoucher.Status != WarehouseVoucherStatusEnum.تحویل_شده)
                            .Sum(v => (decimal?)v.Weight) ?? 0
                        : 0,
                    SourceWarehouseRemainder = db.SupplyFactorItemAllocations.Where(other => a.SourceWarehouseProductId != null
                                                                                             && other.SupplyFactorItemId == a.SupplyFactorItemId &&
                                                                                             other.WarehouseId == a.SourceWarehouseProduct.WarehouseId
                                                                                             && other.FactorItemId == null)
                        .Sum(other => (decimal?)other.Weight) ?? 0,
                    Supplier = sf != null ? sf.SupplierTxt : null,
                    SerialNumber = sf != null ? sf.SerialNumber : null, Date = sf != null ? sf.Date : null,
                    Product = si != null ? si.Product : fi.Product, ProductSmallName = si != null ? si.ProductSmallName : fi.ProductSmallName,
                    SupplyItemWeightCount = si != null ? (decimal?)si.WeightCount : null,
                    SupplyItemUnitTxt = si != null ? si.UnitTxt : fi.UnitTxt, WarehouseTitle = w != null ? w.Title : null,
                    SupplyItemAllocated = db.SupplyFactorItemAllocations.Where(other => a.SupplyFactorItemId != null
                                                                                        && other.SupplyFactorItemId == a.SupplyFactorItemId)
                        .Sum(other => (decimal?)other.Weight) ?? 0
                }).ToListAsync();
            return Ok(new {
                factor, items, allocations,
                isAllocationComplete = items.Count > 0 && items.All(i => i.Allocated >= i.WeightCount)
            });
        }

        [HttpGet]
        [Authorize(Roles = SalesAllocationRoles)]
        public async Task<IActionResult> SearchAllocationSupplyItems(string search, Guid factorItemId, Guid? allocationId = null)
        {
            var sales = await db.FactorItems.AsNoTracking().Where(i => i.Id == factorItemId
                && db.Factors.Any(f => f.Id == i.factorId)).Select(i => new { i.Unit }).FirstOrDefaultAsync();
            if (sales == null) return NotFound("آیتم فروش یافت نشد.");
            var unit = (UnitTypeEnums)sales.Unit;
            search = search?.Trim();
            var excludedId = allocationId ?? Guid.Empty;
            var query = from i in db.SupplyFactorItems.AsNoTracking()
                join f in db.SupplyFactors.AsNoTracking() on i.SupplyFactorId equals f.Id
                where i.Unit == unit && !db.SupplyFactorItemAllocations.Any(a => a.FactorItemId == factorItemId
                                                               && a.SupplyFactorItemId == i.Id && a.SourceWarehouseProductId == null &&
                                                               a.Id != excludedId)
                select new {
                    i.Id, i.Product, i.ProductSmallName, i.WeightCount, i.UnitTxt,
                    Supplier = f.SupplierTxt, f.SerialNumber, f.Date, i.CreateDate,
                    Allocated = i.Allocations.Sum(a => (decimal?)a.Weight) ?? 0,
                    EditingWeight = i.Allocations
                        .Where(a => a.Id == excludedId && a.FactorItemId == factorItemId && a.SourceWarehouseProductId == null)
                        .Sum(a => (decimal?)a.Weight) ?? 0
                };
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(i => i.Supplier.Contains(search) || i.SerialNumber.Contains(search)
                                                                     || i.Product.Contains(search) || i.ProductSmallName.Contains(search));
            var items = await query.OrderByDescending(i => i.CreateDate).ThenByDescending(i => i.Id).Take(201).ToListAsync();
            var warehouses = await db.Warehouses.AsNoTracking()
                .Where(w => (w.Id == SupplyFactorWarehouseService.WasteId && w.WarehouseType == WarehouseType.انبار_ضایعات)
                            || (w.Id == SupplyFactorWarehouseService.StorageId && w.WarehouseType == WarehouseType.انبار_ذخیره))
                .Select(w => new { w.Id, w.Title, w.WarehouseType }).ToListAsync();
            var inventory = from p in db.WarehouseProducts.AsNoTracking()
                join si in db.SupplyFactorItems on p.SupplyFactorItemId equals si.Id
                join sf in db.SupplyFactors on si.SupplyFactorId equals sf.Id
                join w in db.Warehouses on p.WarehouseId equals w.Id
                where p.Unit == unit && si.Unit == unit && ((w.Id == SupplyFactorWarehouseService.WasteId && w.WarehouseType == WarehouseType.انبار_ضایعات)
                       || (w.Id == SupplyFactorWarehouseService.StorageId && w.WarehouseType == WarehouseType.انبار_ذخیره))
                      && !db.SupplyFactorItemAllocations.Any(a => a.SourceWarehouseProductId == p.Id
                                                                  && a.FactorItemId == factorItemId && a.Id != excludedId)
                select new {
                    p.Id, p.WarehouseId, WarehouseTitle = w.Title, p.SupplyFactorItemId,
                    Customer = sf.SupplierTxt, sf.SerialNumber, sf.Date, Product = p.ProductDescription,
                    p.ProductSmallName, p.UnitTxt, p.CreateDate,
                    Stock = p.Transactions.Sum(t => t.TransactionType == WarehouseTransactionTypeEnum.ورود ? (decimal?)t.Weight : -t.Weight) ?? 0,
                    Reserved = p.WarehouseProductVouchers.Where(v => v.WarehouseVoucher.Status != WarehouseVoucherStatusEnum.تحویل_شده)
                        .Sum(v => (decimal?)v.Weight) ?? 0,
                    Remainder = db.SupplyFactorItemAllocations
                        .Where(a => a.SupplyFactorItemId == si.Id && a.WarehouseId == w.Id && a.FactorItemId == null)
                        .Sum(a => (decimal?)a.Weight) ?? 0,
                    Allocated = db.SupplyFactorItemAllocations.Where(a => a.SourceWarehouseProductId == p.Id)
                        .Sum(a => (decimal?)a.Weight) ?? 0,
                    EditingWeight = db.SupplyFactorItemAllocations.Where(a =>
                            a.Id == excludedId && a.FactorItemId == factorItemId && a.SourceWarehouseProductId == p.Id)
                        .Sum(a => (decimal?)a.Weight) ?? 0
                };
            if (!string.IsNullOrWhiteSpace(search))
                inventory = inventory.Where(i => i.Customer.Contains(search)
                                                 || i.SerialNumber.Contains(search) || i.Product.Contains(search) ||
                                                 i.ProductSmallName.Contains(search) || i.WarehouseTitle.Contains(search));
            var stockItems = await inventory.OrderByDescending(i => i.CreateDate).ThenByDescending(i => i.Id).Take(201).ToListAsync();
            return Ok(new {
                warehouses, hasMore = items.Count > 200, warehouseHasMore = stockItems.Count > 200,
                warehouseItems = stockItems.Take(200).Select(i => new {
                    i.Id, i.WarehouseId, i.WarehouseTitle, i.SupplyFactorItemId,
                    i.Customer, i.SerialNumber, i.Date, i.Product, i.ProductSmallName, i.UnitTxt, WeightCount = i.Stock, i.Allocated,
                    Remaining = Math.Max(0, Math.Min(i.Stock - i.Reserved, i.Remainder)),
                    Available = Math.Max(0, Math.Min(i.Stock - i.Reserved + i.EditingWeight, i.Remainder + i.EditingWeight))
                }),
                items = items.Take(200).Select(i => new {
                    i.Id, i.Product, i.ProductSmallName, i.WeightCount, i.UnitTxt,
                    Customer = i.Supplier, i.SerialNumber, i.Date, i.Allocated,
                    Remaining = Math.Max(0, i.WeightCount - i.Allocated),
                    Available = Math.Max(0, i.WeightCount - i.Allocated + i.EditingWeight)
                })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SalesAllocationRoles)]
        public Task<IActionResult> AddSalesFactorAllocation([FromBody] SupplyFactorAllocationRequest model)
            => SaveSalesFactorAllocation(model, null);

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SalesAllocationRoles)]
        public Task<IActionResult> UpdateSalesFactorAllocation([FromBody] SupplyFactorAllocationRequest model)
            => SaveSalesFactorAllocation(model, model?.Id ?? Guid.Empty);

        private static decimal AllocationSalesCapacity(string total, decimal allocated, decimal restored = 0)
            => TryParseAllocationSalesWeight(total, out var weight) ? Math.Max(0, weight - allocated + restored) : 0;

        private static bool TryParseAllocationSalesWeight(string value, out decimal weight)
        {
            var normalized = (value ?? "").Trim().Replace(",", "").Replace("٬", "")
                .Replace("٫", ".").Replace(" ", "").Replace("\u00a0", "");
            for (var i = 0; i <= 9; i++)
                normalized = normalized.Replace((char)('۰' + i), (char)('0' + i))
                    .Replace((char)('٠' + i), (char)('0' + i));
            return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out weight);
        }

        public class SupplyFactorAllocationRequest {
            public Guid Id { get; set; }
            public Guid SupplyFactorItemId { get; set; }
            public Guid? FactorItemId { get; set; }
            public Guid? WarehouseId { get; set; }
            public Guid? SourceWarehouseProductId { get; set; }
            public decimal Weight { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
        public Task<IActionResult> AddSupplyFactorAllocation([FromBody] SupplyFactorAllocationRequest model)
            => SaveSupplyFactorAllocation(model, null);

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
        public Task<IActionResult> UpdateSupplyFactorAllocation([FromBody] SupplyFactorAllocationRequest model)
            => SaveSupplyFactorAllocation(model, model?.Id ?? Guid.Empty);

        private async Task<IActionResult> SaveSupplyFactorAllocation(SupplyFactorAllocationRequest model, Guid? allocationId)
        {
            try
            {
                return await SaveSupplyFactorAllocationCore(model, allocationId);
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }
        }

        private async Task<IActionResult> SaveSupplyFactorAllocationCore(SupplyFactorAllocationRequest model, Guid? allocationId)
        {
            if (!ModelState.IsValid || model == null || model.Weight <= 0
                || decimal.Round(model.Weight, 2) != model.Weight)
                return BadRequest("مقدار تخصیص باید مثبت و حداکثر دارای دو رقم اعشار باشد.");

            var toWarehouse = model.WarehouseId.HasValue;
            if (toWarehouse == model.FactorItemId.HasValue || model.FactorItemId == Guid.Empty
                                                           || toWarehouse && !SupplyFactorWarehouseService.IsAllocationDestination(model.WarehouseId))
                return BadRequest("فقط یک مقصد معتبر (آیتم فروش، انبار ضایعات یا انبار ذخیره) انتخاب کنید.");
            if (toWarehouse && !await db.Warehouses.AnyAsync(w => w.Id == model.WarehouseId
                                                                  && ((w.Id == SupplyFactorWarehouseService.WasteId &&
                                                                       w.WarehouseType == WarehouseType.انبار_ضایعات)
                                                                      || (w.Id == SupplyFactorWarehouseService.StorageId &&
                                                                          w.WarehouseType == WarehouseType.انبار_ذخیره))))
                return BadRequest("انبار مقصد معتبر نیست.");
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            // Serialize allocations for the same supply item, including concurrent requests.
            var item = await db.SupplyFactorItems.FromSqlInterpolated(
                    $"SELECT * FROM [SupplyFactorItems] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {model.SupplyFactorItemId}")
                .FirstOrDefaultAsync();
            if (item == null || !await db.SupplyFactors.AnyAsync(x => x.Id == item.SupplyFactorId))
                return NotFound("آیتم تامین یافت نشد.");
            decimal salesWeight = 0;
            if (!toWarehouse)
            {
                // Serialize requests from different supply items targeting the same sales item.
                var salesItem = await db.FactorItems.FromSqlInterpolated(
                        $"SELECT * FROM [FactorItems] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {model.FactorItemId}")
                    .FirstOrDefaultAsync();
                if (salesItem == null || !await db.Factors.AnyAsync(f => f.Id == salesItem.factorId))
                    return BadRequest("آیتم فاکتور فروش یافت نشد.");
                if (salesItem.Unit != (int)item.Unit)
                    return BadRequest("واحد آیتم فروش و آیتم تامین باید یکسان باشد.");
                if (!TryParseAllocationSalesWeight(salesItem.WeightCount, out salesWeight) || salesWeight <= 0)
                    return BadRequest("مقدار آیتم فاکتور فروش معتبر نیست؛ ابتدا فاکتور فروش را اصلاح کنید.");
            }

            SupplyFactorItemAllocation allocation = null;
            if (allocationId.HasValue)
            {
                allocation = await db.SupplyFactorItemAllocations
                    .FirstOrDefaultAsync(x => x.Id == allocationId.Value && x.SupplyFactorItemId == item.Id);
                if (allocation == null) return NotFound("تخصیص یافت نشد؛ ممکن است قبلاً حذف شده باشد.");
            }

            var excludedId = allocationId ?? Guid.Empty;
            if (!toWarehouse)
            {
                var salesAllocated = await db.SupplyFactorItemAllocations
                    .Where(x => x.FactorItemId == model.FactorItemId && x.Id != excludedId)
                    .SumAsync(x => (decimal?)x.Weight) ?? 0;
                if (model.Weight > salesWeight - salesAllocated)
                    return BadRequest("مقدار تخصیص از باقی‌مانده آیتم فروش در مجموع تمام فاکتورهای تامین بیشتر است.");
            }

            if (await db.SupplyFactorItemAllocations.AnyAsync(x => x.SupplyFactorItemId == item.Id
                                                                   && x.FactorItemId == model.FactorItemId && x.WarehouseId == model.WarehouseId
                                                                   && x.SourceWarehouseProductId == (toWarehouse ? null :
                                                                       allocation != null ? allocation.SourceWarehouseProductId : null) &&
                                                                   x.Id != excludedId))
                return BadRequest("این مقصد قبلاً به همین آیتم تامین تخصیص داده شده است؛ تخصیص قبلی را ویرایش کنید.");
            var allocated = await db.SupplyFactorItemAllocations
                .Where(x => x.SupplyFactorItemId == item.Id && x.Id != excludedId)
                .SumAsync(x => (decimal?)x.Weight) ?? 0;
            var warehouseSource = allocation?.SourceWarehouseProductId;
            if (warehouseSource == null && model.Weight > item.WeightCount - allocated)
                return BadRequest("مقدار تخصیص از مقدار باقی‌مانده آیتم تامین بیشتر است.");
            if (warehouseSource != null && toWarehouse)
                return BadRequest("برای بازگرداندن برداشت به انبار، تخصیص فروش را حذف کنید.");
            if (warehouseSource != null)
            {
                try
                {
                    await supplyWarehouse.ChangeWarehouseSourceAsync(warehouseSource, allocation.Weight,
                        toWarehouse ? null : warehouseSource, model.Weight);
                }
                catch (InvalidOperationException e)
                {
                    return BadRequest(e.Message);
                }

                if (toWarehouse) allocation.SourceWarehouseProductId = null;
            }

            if (allocation == null)
            {
                db.SupplyFactorItemAllocations.Add(new SupplyFactorItemAllocation {
                    SupplyFactorItemId = item.Id, FactorItemId = model.FactorItemId, WarehouseId = model.WarehouseId, Weight = model.Weight
                });
            } else
            {
                allocation.FactorItemId = model.FactorItemId;
                allocation.WarehouseId = model.WarehouseId;
                allocation.Weight = model.Weight;
            }

            await db.SaveChangesAsync();
            try
            {
                await supplyWarehouse.SynchronizeAsync(item, await userManager.GetUserAsync(HttpContext.User));
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }

            await transaction.CommitAsync();
            return Ok(new { supplyFactorId = item.SupplyFactorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
        public async Task<IActionResult> DeleteSupplyFactorAllocation([FromBody] SupplyFactorAllocationRequest model)
        {
            if (!ModelState.IsValid || model == null) return BadRequest();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var item = await db.SupplyFactorItems.FromSqlInterpolated(
                    $"SELECT * FROM [SupplyFactorItems] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {model.SupplyFactorItemId}")
                .FirstOrDefaultAsync();
            if (item == null || !await db.SupplyFactors.AnyAsync(x => x.Id == item.SupplyFactorId))
                return NotFound("آیتم تامین یافت نشد.");
            var allocation = await db.SupplyFactorItemAllocations
                .FirstOrDefaultAsync(x => x.Id == model.Id && x.SupplyFactorItemId == item.Id);
            if (allocation == null) return NotFound("تخصیص یافت نشد؛ ممکن است قبلاً حذف شده باشد.");
            try
            {
                await supplyWarehouse.ChangeWarehouseSourceAsync(allocation.SourceWarehouseProductId, allocation.Weight, null, 0);
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }

            db.SupplyFactorItemAllocations.Remove(allocation);
            await db.SaveChangesAsync();
            try
            {
                await supplyWarehouse.SynchronizeAsync(item, await userManager.GetUserAsync(HttpContext.User));
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }

            await transaction.CommitAsync();
            return Ok(new { supplyFactorId = item.SupplyFactorId });
        }

        private async Task<IActionResult> SaveSalesFactorAllocation(SupplyFactorAllocationRequest model, Guid? allocationId)
        {
            if (!ModelState.IsValid || model == null || model.Weight <= 0 || decimal.Round(model.Weight, 2) != model.Weight)
                return BadRequest("مقدار تخصیص باید مثبت و حداکثر دارای دو رقم اعشار باشد.");
            if (!model.FactorItemId.HasValue || model.FactorItemId == Guid.Empty)
                return BadRequest("آیتم فروش معتبر نیست.");
            var warehouse = model.SourceWarehouseProductId.HasValue;
            if (!warehouse && (model.WarehouseId.HasValue || model.SupplyFactorItemId == Guid.Empty)
                || warehouse && model.SupplyFactorItemId != Guid.Empty)
                return BadRequest("یک آیتم تامین یا موجودی مشخصی از انبار ضایعات / ذخیره انتخاب کنید.");
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var previous = allocationId.HasValue
                ? await db.SupplyFactorItemAllocations.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == allocationId && a.FactorItemId == model.FactorItemId)
                : null;
            if (allocationId.HasValue && previous == null) return NotFound("تخصیص این آیتم فروش یافت نشد.");
            var warehouseProduct = warehouse
                ? await db.WarehouseProducts.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == model.SourceWarehouseProductId && p.SupplyFactorItemId != null)
                : null;
            if (warehouse && (warehouseProduct == null || !SupplyFactorWarehouseService.IsAllocationDestination(warehouseProduct.WarehouseId)
                                                       || model.WarehouseId.HasValue && model.WarehouseId != warehouseProduct.WarehouseId))
                return BadRequest("موجودی انبار مرتبط با آیتم تامین یافت نشد.");
            Guid? targetSourceId = warehouse ? warehouseProduct.SupplyFactorItemId : model.SupplyFactorItemId;
            var sources = new List<SupplyFactorItem>();
            // The source-before-sales lock order is shared with the existing supply workspace.
            foreach (var sourceId in new[] { targetSourceId, previous?.SupplyFactorItemId }.Where(id => id.HasValue).Distinct().OrderBy(id => id))
            {
                var source = await db.SupplyFactorItems.FromSqlInterpolated(
                    $"SELECT * FROM [SupplyFactorItems] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {sourceId}").FirstOrDefaultAsync();
                if (source == null || !await db.SupplyFactors.AnyAsync(f => f.Id == source.SupplyFactorId))
                    return NotFound("آیتم تامین یافت نشد.");
                sources.Add(source);
            }

            var sales = await db.FactorItems.FromSqlInterpolated(
                $"SELECT * FROM [FactorItems] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {model.FactorItemId}").FirstOrDefaultAsync();
            if (sales == null || !await db.Factors.AnyAsync(f => f.Id == sales.factorId)) return NotFound("آیتم فروش یافت نشد.");
            if (sources.Single(i => i.Id == targetSourceId).Unit != (UnitTypeEnums)sales.Unit
                || warehouseProduct != null && warehouseProduct.Unit != (UnitTypeEnums)sales.Unit)
                return BadRequest("واحد آیتم فروش باید با واحد آیتم تامین و موجودی انبار انتخاب‌شده یکسان باشد.");
            if (!TryParseAllocationSalesWeight(sales.WeightCount, out var total) || total <= 0)
                return BadRequest("مقدار آیتم فروش معتبر نیست؛ ابتدا فاکتور فروش را اصلاح کنید.");
            var excluded = allocationId ?? Guid.Empty;
            var allocation = allocationId.HasValue
                ? await db.SupplyFactorItemAllocations
                    .FirstOrDefaultAsync(a => a.Id == excluded && a.FactorItemId == sales.Id)
                : null;
            if (allocationId.HasValue && (allocation == null || allocation.SupplyFactorItemId != previous.SupplyFactorItemId))
                return BadRequest("تخصیص تغییر کرده است؛ اطلاعات را دوباره دریافت کنید.");
            var salesAllocated = await db.SupplyFactorItemAllocations.Where(a => a.FactorItemId == sales.Id && a.Id != excluded)
                .SumAsync(a => (decimal?)a.Weight) ?? 0;
            if (model.Weight > total - salesAllocated) return BadRequest("مقدار از باقی‌مانده آیتم فروش در مجموع تمام تخصیص‌ها بیشتر است.");
            if (await db.SupplyFactorItemAllocations.AnyAsync(a => a.FactorItemId == sales.Id && a.Id != excluded
                                                                                              && a.SupplyFactorItemId == targetSourceId &&
                                                                                              a.WarehouseId == null &&
                                                                                              a.SourceWarehouseProductId ==
                                                                                              model.SourceWarehouseProductId))
                return BadRequest("این آیتم تامین یا انبار قبلاً ثبت شده است؛ تخصیص قبلی را ویرایش کنید.");
            if (!warehouse && targetSourceId.HasValue)
            {
                var sourceAllocated = await db.SupplyFactorItemAllocations.Where(a => a.SupplyFactorItemId == targetSourceId && a.Id != excluded)
                    .SumAsync(a => (decimal?)a.Weight) ?? 0;
                if (previous?.SourceWarehouseProductId != null && previous.SupplyFactorItemId == targetSourceId)
                    sourceAllocated += previous.Weight; // Returning the old withdrawal restores its warehouse remainder.
                if (model.Weight > sources.Single(i => i.Id == targetSourceId).WeightCount - sourceAllocated)
                    return BadRequest("مقدار از باقی‌مانده آیتم تامین در مجموع تمام تخصیص‌ها بیشتر است.");
            }

            try
            {
                await supplyWarehouse.ChangeWarehouseSourceAsync(previous?.SourceWarehouseProductId, previous?.Weight ?? 0,
                    model.SourceWarehouseProductId, model.Weight);
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }

            if (allocation == null)
            {
                allocation = new SupplyFactorItemAllocation();
                db.SupplyFactorItemAllocations.Add(allocation);
            }

            allocation.FactorItemId = sales.Id;
            allocation.SupplyFactorItemId = targetSourceId;
            allocation.WarehouseId = null;
            allocation.SourceWarehouseProductId = model.SourceWarehouseProductId;
            allocation.Weight = model.Weight;
            await db.SaveChangesAsync();
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                foreach (var source in sources.OrderBy(i => i.Id)) await supplyWarehouse.SynchronizeAsync(source, user);
                await supplyWarehouse.ReverseLegacySalesReceiptsAsync(sales, user);
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }

            await transaction.CommitAsync();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SalesAllocationRoles)]
        public async Task<IActionResult> DeleteSalesFactorAllocation([FromBody] SupplyFactorAllocationRequest model)
        {
            if (!ModelState.IsValid || model == null || !model.FactorItemId.HasValue) return BadRequest();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var previous = await db.SupplyFactorItemAllocations.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == model.Id && a.FactorItemId == model.FactorItemId);
            if (previous == null) return NotFound("تخصیص این آیتم فروش یافت نشد.");
            SupplyFactorItem source = null;
            if (previous.SupplyFactorItemId.HasValue)
            {
                source = await db.SupplyFactorItems.FromSqlInterpolated(
                    $"SELECT * FROM [SupplyFactorItems] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {previous.SupplyFactorItemId}").FirstOrDefaultAsync();
                if (source == null) return NotFound("آیتم تامین یافت نشد.");
            }

            var sales = await db.FactorItems.FromSqlInterpolated(
                $"SELECT * FROM [FactorItems] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {model.FactorItemId}").FirstOrDefaultAsync();
            if (sales == null || !await db.Factors.AnyAsync(f => f.Id == sales.factorId)) return NotFound("آیتم فروش یافت نشد.");
            var allocation = await db.SupplyFactorItemAllocations.FirstOrDefaultAsync(a => a.Id == model.Id
                                                                                           && a.FactorItemId == sales.Id &&
                                                                                           a.SupplyFactorItemId == previous.SupplyFactorItemId);
            if (allocation == null) return NotFound("تخصیص تغییر کرده است؛ اطلاعات را دوباره دریافت کنید.");
            try
            {
                await supplyWarehouse.ChangeWarehouseSourceAsync(allocation.SourceWarehouseProductId, allocation.Weight, null, 0);
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }

            db.SupplyFactorItemAllocations.Remove(allocation);
            await db.SaveChangesAsync();
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                if (source != null) await supplyWarehouse.SynchronizeAsync(source, user);
                await supplyWarehouse.ReverseLegacySalesReceiptsAsync(sales, user);
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }

            await transaction.CommitAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> AddSupplyFactor([FromBody] SupplyFactor model)
        {
            if (model?.Items == null || !model.Items.Any() || model.Items.Any(i => i.WeightCount <= 0 || i.IsDeleted))
                return BadRequest("آیتم‌های تامین و مقدار مثبت آن‌ها الزامی است.");
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
                model.DateMiladi = dtService.GetDateMiladi(model.Date) ?? DateTime.Now;

                await db.SupplyFactors.AddAsync(model);

                var attach = fileSaver.SaveAttachments(model.Attachments, _AttachType.SupplyFactor);
                foreach (var att in attach)
                {
                    att.EntityId = model.Id;
                }

                await db.Attachments.AddRangeAsync(attach);

                await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);

                await db.SaveChangesAsync();
                foreach (var supplyItem in model.Items.OrderBy(i => i.Id))
                    await supplyWarehouse.SynchronizeAsync(supplyItem, user);
                await transaction.CommitAsync();
                return Ok();
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return BadRequest(e is InvalidOperationException ? e.Message : "ثبت فاکتور تامین انجام نشد.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSupplyFactor([FromBody] SupplyFactor model)
        {
            if (model == null) return BadRequest();
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                model.DateMiladi = dtService.GetDateMiladi(model.Date) ?? DateTime.Now;
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;

                var entity = await db.SupplyFactors
                    .Include(x => x.SupplyFactorOrderHeads)
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == model.Id);

                if (entity == null) return NotFound("رکوردی یافت نشد!");

                foreach (var existingItem in entity.Items.OrderBy(x => x.Id))
                {
                    await db.SupplyFactorItems.FromSqlInterpolated(
                            $"SELECT * FROM [SupplyFactorItems] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {existingItem.Id}")
                        .LoadAsync();
                    var allocated = await db.SupplyFactorItemAllocations
                        .Where(x => x.SupplyFactorItemId == existingItem.Id).SumAsync(x => (decimal?)x.Weight) ?? 0;
                    var replacement = model.Items?.FirstOrDefault(x => x.Id == existingItem.Id && !x.IsDeleted);
                    if (allocated > 0 && (replacement == null || replacement.WeightCount < allocated))
                        return BadRequest("آیتم دارای تخصیص را نمی‌توان حذف کرد یا مقدار آن را کمتر از مجموع تخصیص‌ها قرار داد.");
                    if (allocated > 0 && (replacement.ProductCode != existingItem.ProductCode
                                          || replacement.Product != existingItem.Product || replacement.Unit != existingItem.Unit))
                        return BadRequest("برای تغییر محصول یا واحد آیتم، ابتدا تخصیص‌های آن را حذف کنید.");
                }

                await _logSystemService.LogChange(entity, model, LogActionEnum.ویرایش, user, ["SupplyFactorOrderHeads", "Items"]);
                db.Entry(entity).CurrentValues.SetValues(model);

                entity.SupplyFactorOrderHeads = model.SupplyFactorOrderHeads;
                var incomingItems = model.Items ?? [];
                if (incomingItems.Any(i => i.Id != Guid.Empty && !entity.Items.Any(existing => existing.Id == i.Id)))
                    return BadRequest("آیتم ارسالی متعلق به این فاکتور تامین نیست.");
                if (incomingItems.Any(i => !i.IsDeleted && i.WeightCount <= 0))
                    return BadRequest("مقدار آیتم تامین باید مثبت باشد.");
                foreach (var existingItem in entity.Items.ToList())
                {
                    var replacement = incomingItems.FirstOrDefault(i => i.Id == existingItem.Id && !i.IsDeleted);
                    if (replacement == null) existingItem.IsDeleted = true;
                    else
                    {
                        db.Entry(existingItem).CurrentValues.SetValues(replacement);
                        existingItem.SupplyFactorId = entity.Id;
                    }
                }

                foreach (var added in incomingItems.Where(i => i.Id == Guid.Empty && !i.IsDeleted))
                {
                    added.SupplyFactorId = entity.Id;
                    entity.Items.Add(added);
                }


                var attach = fileSaver.SaveAttachments(model.Attachments, _AttachType.SupplyFactor, null, false);
                foreach (var att in attach.Where(f => f.EntityId == null))
                {
                    att.EntityId = model.Id;
                }

                db.Attachments.UpdateRange(attach);

                await db.SaveChangesAsync();
                foreach (var supplyItem in entity.Items.OrderBy(i => i.Id))
                    await supplyWarehouse.SynchronizeAsync(supplyItem, user);
                await transaction.CommitAsync();
                return Ok();
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return BadRequest(e is InvalidOperationException ? e.Message : "ویرایش فاکتور تامین انجام نشد.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddPayment([FromBody] Payment model)
        {
            if (model == null) return BadRequest();
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;


                model.PaymentDate = dtService.GetDateMiladi(model.PaymentDateTxt) ?? DateTime.Now;
                model.PaymentTypeTxt = model.PaymentType.ToString().Replace("_", " ");
                await db.Payments.AddAsync(model);
                await db.SaveChangesAsync();

                var attach = fileSaver.SaveAttachments(model.Attachments, _AttachType.Payment);
                foreach (var att in attach)
                {
                    att.EntityId = model.Id;
                }

                await db.Attachments.AddRangeAsync(attach);

                await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok();
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePayment([FromBody] Payment model)
        {
            if (model == null) return BadRequest();
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                model.PaymentDate = dtService.GetDateMiladi(model.PaymentDateTxt) ?? DateTime.Now;
                model.PaymentTypeTxt = model.PaymentType.ToString().Replace("_", " ");

                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;

                var entity = await db.Payments.Include(x => x.PaymentSupplyFactors).FirstOrDefaultAsync(x => x.Id == model.Id);
                if (entity == null) return NotFound("رکوردی یافت نشد!");

                await _logSystemService.LogChange(entity, model, LogActionEnum.ویرایش, user, ["PaymentSupplyFactors"]);

                entity.PaymentSupplyFactors = model.PaymentSupplyFactors;
                db.Entry(entity).CurrentValues.SetValues(model);

                var attach = fileSaver.SaveAttachments(model.Attachments, _AttachType.Payment);
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
                return BadRequest();
            }
        }

        public async Task<IActionResult> GetPayment(Guid? paymentId)
        {
            try
            {
                var query = db.Payments.AsNoTracking().Where(f => f.Id == paymentId);

                var data = await query
                    .Include(f => f.PaymentSupplyFactors)
                    .AsNoTracking()
                    .Select(p => new {
                        Payment = p,
                        SupplyFactorSerials = p.PaymentSupplyFactors
                            .Join(db.SupplyFactors,
                                pSf => pSf.SupplyFactorId,
                                sf => sf.Id,
                                (pSf, sf) => sf.SerialNumber
                            )
                            .ToList(),
                        Attachments = db.Attachments
                            .Where(a => a.EntityId == p.Id && a._AttachType == _AttachType.Payment)
                            .ToList()
                    })
                    .FirstOrDefaultAsync();
                return Ok(data);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        public async Task<IActionResult> GetAllSupplyPaymentDt()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
                var model = JsonConvert.DeserializeObject<FilterVModel>(Request.Form["model"].FirstOrDefault());
                var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault()?.ToLower().Trim();

                var query = db.Payments.OrderByDescending(f => f.CreateDate).AsNoTracking();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    var searchNumber = TextService.convertToInt(searchValue);
                    query = query.Where(f =>
                        f.Amount == searchNumber ||
                        f.PaymentNumber == searchNumber ||
                        f.FromAccountTxt.Contains(searchValue) ||
                        f.ToAccountTxt.Contains(searchValue)
                    );
                }

                var recordsTotal = query.Count();


                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    var isAscending = sortColumnDirection == "asc";
                    query = sortColumn switch {
                        "PaymentNumber" => isAscending
                            ? query.OrderBy(f => f.PaymentNumber)
                            : query.OrderByDescending(f => f.PaymentNumber),
                        "Amount" => isAscending
                            ? query.OrderBy(f => f.Amount)
                            : query.OrderByDescending(f => f.Amount),
                        "CreateDateTxt" => isAscending
                            ? query.OrderBy(f => f.CreateDate)
                            : query.OrderByDescending(f => f.CreateDate),
                        "PaymentDateTxt" => isAscending
                            ? query.OrderBy(f => f.PaymentDate)
                            : query.OrderByDescending(f => f.PaymentDate),
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
            catch (Exception)
            {
                return BadRequest();
            }
        }

        public async Task<IActionResult> GetSupplierFinancialSummary(Guid supplierId)
        {
            /*var totalAmount = await db.SupplyFactors
                .AsNoTracking()
                .Where(f => f.SupplierId == supplierId)
                .SumAsync(f => f.Amount);*/


            /*var grouped = await db.SupplyFactors
                .AsNoTracking()
                .Where(f => f.SupplierId == supplierId)
                .GroupBy(f => f.OfficialType)
                .Select(g => new {
                    OfficialType = g.Key,
                    TotalAmount = g.Sum(f => f.Amount)
                })
                .ToListAsync();

            var data = new {
                official = grouped.FirstOrDefault(x => x.OfficialType == 1)?.TotalAmount ?? 0,
                notOfficial = grouped.FirstOrDefault(x => x.OfficialType == 0)?.TotalAmount ?? 0
            };*/

            var summary = await db
                .GetSupplierFinancialSummaries()
                .FirstOrDefaultAsync(x => x.SupplierId == supplierId);

            return Ok(summary);
        }

        public async Task<IActionResult> DeleteSupplyFactor(Guid id)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.SupplyFactors.Include(f => f.Items).FirstOrDefaultAsync(f => f.Id == id);
            if (item == null) return NotFound();
            foreach (var supplyItem in item.Items.OrderBy(i => i.Id))
            {
                await db.SupplyFactorItems.FromSqlInterpolated(
                    $"SELECT * FROM [SupplyFactorItems] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {supplyItem.Id}").LoadAsync();
                if (await db.SupplyFactorItemAllocations.AnyAsync(a => a.SupplyFactorItemId == supplyItem.Id))
                    return BadRequest("برای حذف فاکتور تامین، ابتدا تخصیص‌های آن را حذف کنید.");
                supplyItem.IsDeleted = true;
            }

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;
            await _logSystemService.LogChange(item, null, LogActionEnum.حذف, user);
            await db.SaveChangesAsync();
            try
            {
                foreach (var supplyItem in item.Items.OrderBy(i => i.Id))
                    await supplyWarehouse.SynchronizeAsync(supplyItem, user);
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }

            await transaction.CommitAsync();
            return Ok();
        }

        public async Task<IActionResult> GetAllSupplyFactorsPartial(string search, Guid? dealId = null)
        {
            try
            {
                var query = db.SupplyFactors.Include(f => f.Deal).AsNoTracking();

                if (dealId != null)
                {
                    query = query.Where(f => f.DealId == dealId);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    var searchNumber = TextService.convertToInt(search);
                    query = query.Where(f =>
                        f.SerialNumber == search ||
                        f.SupplierTxt.Contains(search) ||
                        f.SupplyFactorOrderHeads.Any(sfOh =>
                            db.OrderHeads.Any(oh => oh.Id == sfOh.OrderHeadId && oh.OrderNumber == searchNumber)
                        )
                    );
                }

                var data = await query.OrderByDescending(f => f.CreateDate)
                    .Select(sf => new SupplyFactorVModel {
                        IsAllocationComplete = sf.Items.Any() && sf.Items.All(i =>
                            (i.Allocations.Sum(a => (decimal?)a.Weight) ?? 0) >= i.WeightCount),
                        SupplyFactor = sf,
                        OrderSerials = sf.SupplyFactorOrderHeads
                            .Join(db.OrderHeads,
                                sfOh => sfOh.OrderHeadId,
                                oh => oh.Id,
                                (sfOh, oh) => oh.OrderNumber
                            ).ToList()
                    })
                    .ToListAsync();

                return PartialView(data);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetAllSupplyFactorsTablePartial([FromBody] GetAllBySelectedVModel model)
        {
            try
            {
                var search = model.Search;

                var query = db.SupplyFactors
                    .OrderByDescending(f => model.SelectedIds.Contains(f.Id))
                    .ThenByDescending(f => f.CreateDate)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(f =>
                        f.SupplierTxt.Contains(search) ||
                        f.SerialNumber == search ||
                        f.PaymentAmount == TextService.convertToInt(search)
                    );
                } else
                {
                    query = query.Take(50);
                }

                var data = await query.ToListAsync();
                return PartialView(data);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        public async Task<IActionResult> GetSupplyFactor(Guid id)
        {
            try
            {
                var data = await db.SupplyFactors.AsNoTracking()
                    .Where(f => f.Id == id)
                    .Include(f => f.SupplyFactorOrderHeads)
                    .Include(f => f.Items)
                    .Select(sf => new {
                        SupplyFactor = sf,
                        OrderSerials = sf.SupplyFactorOrderHeads
                            .Join(db.OrderHeads,
                                sfo => sfo.OrderHeadId,
                                oh => oh.Id,
                                (sfo, oh) => oh.OrderNumber
                            ).ToList(),
                        Attachments = db.Attachments
                            .Where(a => a.EntityId == sf.Id && a._AttachType == _AttachType.SupplyFactor)
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                return Ok(data);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        public async Task<IActionResult> GetSupplyBaseData()
        {
            var extraBaseItems = await db.BaseItems
                .Where(f => f.TypeCode == BaseItemEnum.هزینه_اضافی_تولید)
                .Select(f => new {
                    Value = f.Id,
                    Text = f.Title
                }).ToListAsync();

            var suppliers = await db.Customers
                .Where(f => f.RoleType == RoleTypeEnum.تامین_کننده || f.RoleType == RoleTypeEnum.تامین_کننده_مشتری)
                .OrderByDescending(f => f.CreateDate)
                .Select(f => new {
                    Value = f.Id,
                    Text = $"{f.CompanyName ?? ""} {f.AgentName ?? ""} ({f.CustomerCode})".Trim(),
                })
                .ToListAsync();

            var model = new { extraBaseItems, suppliers };
            return Ok(model);
        }

        public async Task<IActionResult> GetGroupedOrdersBySupplier()
        {
            try
            {
                var result = (
                        from o in db.Orders
                        join oh in db.OrderHeads on o.OrderHeadId equals oh.Id
                        where oh.Status != OrderHeadStatusEnum.تحویل_شده && oh.SupplierId != null
                        select new { o, oh }
                    )
                    .AsEnumerable()
                    .GroupBy(x => new { x.oh.SupplierId, x.oh.SupplierTxt })
                    .Select(g => new GroupedOrdersBySupplierVModal {
                        SupplierId = g.Key.SupplierId,
                        SupplierTxt = g.Key.SupplierTxt,
                        Products = g.GroupBy(p => p.o.ProductCode).Select(x => new ProductGroupVModel {
                                Orders = x.Select(z => z.o).ToList(),
                                OrderHead = x.Select(z => z.oh).FirstOrDefault(),
                                SumWeight = x.Sum(f => int.TryParse(f.o.Weight, out var w) ? w : 0),
                                ProductSmallName = x.Select(g => g.o.ProductSmallName).FirstOrDefault(),
                                UnitTxt = x.Select(g => g.o.UnitTxt).FirstOrDefault(),
                            })
                            .ToList()
                    })
                    .ToList();

                /*var product = ordersList.GroupBy(f => f.ProductCode)
                    .Select(g =>
                        new {
                            supplierTxt = groupedOh.FirstOrDefault(f => f.Id == g.Select(x => x.OrderHeadId).FirstOrDefault())?.SupplierTxt,
                            product = g.Select(f => f.ProductSmallName).FirstOrDefault(),
                            weight = g.Sum(f => int.Parse(f.Weight)),
                            weightUnit = g.Select(f => f.UnitTxt).FirstOrDefault(),
                            data = g.ToList()
                        }
                    ).ToList();*/


                return PartialView(result);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddSupply([FromBody] List<Supply> model)
        {
            if (model == null) return BadRequest();

            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var setting = await db.ASettings.FirstOrDefaultAsync();
                foreach (var item in model)
                {
                    if (item.Id == Guid.Empty)
                    {
                        item.UserCreator = user.FullName;
                        item.UserCreatorID = user.Id;

                        db.Supplies.Add(item);
                        await _logSystemService.LogChange(item, null, LogActionEnum.ثبت, user);
                    } else
                    {
                        item.UserModify = user.FullName;
                        item.UserModifyID = user.Id;
                        item.ModifyDate = DateTime.Now;
                        item.ModifyDateTxt = dtService.GetDateTimePersian();

                        var oldSupply = await db.Supplies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.Id);
                        await _logSystemService.LogChange(oldSupply, item, LogActionEnum.ویرایش, user);

                        db.Supplies.Update(item);
                    }
                }

                if (setting.SetAmountByRoleType != SetAmountRoleEnum.فروش)
                {
                    var pf = await db.PreFactors.Where(f => f.Id == model.FirstOrDefault().PfId).FirstOrDefaultAsync();
                    if (pf.TaminStep != TaminStepEnum.تایید_شده)
                    {
                        pf.TaminStep = TaminStepEnum.در_انتظار_تایید;
                        pf.TaminStepTxt = nameof(TaminStepEnum.در_انتظار_تایید).Replace("_", " ");
                    }
                }

                await db.SaveChangesAsync();
                return Ok(true);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplies(Guid pfId)
        {
            var data = await db.Supplies.Where(f => f.PfId == pfId).ToListAsync();

            return Ok(data);
        }


        [HttpPost]
        public async Task<IActionResult> GetSuppliesProfitDt()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();

                var start = int.TryParse(Request.Form["start"].FirstOrDefault(), out var parsedStart)
                    ? Math.Max(0, parsedStart)
                    : 0;

                var length = int.TryParse(Request.Form["length"].FirstOrDefault(), out var parsedLength)
                    ? parsedLength
                    : 10;

                var model = JsonConvert.DeserializeObject<FilterVModel>(
                    Request.Form["model"].FirstOrDefault());

                var sortColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
                var sortColumn = string.IsNullOrWhiteSpace(sortColumnIndex)
                    ? null
                    : Request.Form[$"columns[{sortColumnIndex}][name]"].FirstOrDefault();

                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault()?.Trim();

                // پارامترهای فیلتر تاریخ و کاربر؛ فیلترها داخل Stored Procedure اعمال می‌شوند.
                DateTime? fromDate = null;
                DateTime? toDate = null;
                string userCreatorDealId = null;
                var dateFilterChk = false;
                var userFilterChk = false;

                if (model?.filt != null)
                {
                    dateFilterChk = model.filt.DateFilterChk;
                    userFilterChk = model.filt.SelFilterChd;

                    if (dateFilterChk)
                    {
                        fromDate = dtService.GetDateMiladi(model.filt.fromDate)
                                   ?? dtService.GetDateMiladi("1000/01/01");

                        toDate = (dtService.GetDateMiladi(model.filt.toDate) ?? DateTime.Today)
                            .Date.AddDays(1).AddTicks(-1);
                    }

                    if (userFilterChk)
                        userCreatorDealId = model.filt.selFilter?.ToString();
                }

                var procedureParameters = new {
                    FromDate = fromDate,
                    ToDate = toDate,
                    DateFilterChk = dateFilterChk,
                    UserCreatorDealId = userCreatorDealId,
                    SelFilterChd = userFilterChk,
                    Type = model?._type,
                    SearchValue = string.IsNullOrWhiteSpace(searchValue) ? null : searchValue,
                    Start = start,
                    Length = length,
                    SortColumn = string.IsNullOrWhiteSpace(sortColumn) ? "DealDate" : sortColumn,
                    SortDirection = string.IsNullOrWhiteSpace(sortColumnDirection)
                        ? "desc"
                        : sortColumnDirection
                };

                await using var connection = new SqlConnection(
                    db.Database.GetDbConnection().ConnectionString);

                // SP نتیجه اول را به‌صورت صفحه‌بندی‌شده و نتیجه دوم را به‌صورت مجموع‌ها برمی‌گرداند.
                using var multi = await connection.QueryMultipleAsync(
                    "dbo.Sp_GetSuppliesProfit",
                    procedureParameters,
                    commandType: CommandType.StoredProcedure);

                var data = (await multi.ReadAsync<SuppliesProfitDtVModel>()).ToList();
                var totalsRow = await multi.ReadSingleAsync();

                var recordsFiltered = Convert.ToInt64(totalsRow.RecordsFiltered);

                return Json(new {
                    draw,
                    recordsTotal = recordsFiltered,
                    recordsFiltered,
                    data,
                    totals = new {
                        totalSuppliesAmountAll = Convert.ToDecimal(totalsRow.TotalSuppliesAmountAll),
                        totalFactorsAmountAll = Convert.ToDecimal(totalsRow.TotalFactorsAmountAll),
                        profitAll = Convert.ToDecimal(totalsRow.ProfitAll)
                    }
                });
            }
            catch (Exception ex)
            {
                // در صورت وجود ILogger، خطا را ثبت کن.
                // logger.LogError(ex, "Error in GetSuppliesProfitDt");

                return BadRequest();
            }
        }

        public async Task<IActionResult> GetSuppliesDemandsDt() // مطالبات
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
                var model = JsonConvert.DeserializeObject<FilterVModel>(Request.Form["model"].FirstOrDefault());
                var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault()?.ToLower().Trim();


                var factorQuery = db.PreFactors.AsNoTracking()
                    .Where(pf =>
                        pf.Pftype != (int)PreFactorEnum.فاقد_اعتبار
                        && pf.Pftype != (int)PreFactorEnum.بسته_شده
                        && pf.Factors.Any(f => f.status == FactorStatus.تایید_شده)
                    )
                    .GroupBy(pf => pf.DealId)
                    .Select(g => new {
                        DealId = g.Key,
                        TotalFactorsAmount = g.SelectMany(x => x.Factors)
                            .Where(f => f.status == FactorStatus.تایید_شده)
                            .Sum(f => f.ParentReturnedId == null ? f.PaymentAmount : -f.PaymentAmount)
                    });


                var accQuery = db.Accountancies.AsNoTracking()
                    .Where(acc =>
                        acc.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                        && (acc.AccType == AcEnum.واریزی || acc.AccType == AcEnum.برگشتی)
                        && (acc.PayType == PayTypeEnum.نقدی || acc.PayType == PayTypeEnum.تهاتری)
                        && acc.Status == AccStatusEnum.تایید_شده
                    )
                    .GroupBy(acc => acc.DealId)
                    .Select(g => new {
                        DealId = g.Key,
                        TotalAccAmount = g.Sum(x => x.AccType == AcEnum.برگشتی ? -x.BackWard : x.credit)
                    });


                var query =
                    from deal in db.Deals.AsNoTracking()
                    join f in factorQuery on deal.Id equals f.DealId
                    join acc in accQuery on deal.Id equals acc.DealId
                    select new SuppliesDemandsDtVModel {
                        DealId = deal.Id,
                        DealSerialNumber = deal.SerialNumber,
                        DealDate = deal.CreateDateTxt.Substring(0, 10),
                        DealDateM = deal.CreateDate,
                        CustomerName = deal.CustomerName,
                        UserCreatorDeal = deal.UserCreator,
                        UserCreatorDealId = deal.UserCreatorID,

                        TotalFactorsAmount = f.TotalFactorsAmount,
                        TotalAccAmount = acc.TotalAccAmount ?? 0,
                        Demand = f.TotalFactorsAmount - (acc.TotalAccAmount ?? 0)
                    };


                if (model?.filt != null)
                {
                    query = query.ApplyFilter(model, null, f => f.DealDateM, f => f.UserCreatorDealId);
                    /*var fd = dtService.GetDateMiladi(model.filt.fromDate) ?? dtService.GetDateMiladi("1000/01/01");
                    var td = dtService.GetDateMiladi(model.filt.toDate) ?? DateTime.Now;

                    if (model.filt.DateFilterChk)
                        query = query.Where(f => f.DealDateM >= fd && f.DealDateM <= td);
                    if (model.filt.SelFilterChd)
                        query = query.Where(f => f.UserCreatorDealId == model.filt.selFilter.ToString());*/
                }


                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query
                        .Where(f =>
                            f.DealSerialNumber.ToString() == searchValue ||
                            f.CustomerName.Contains(searchValue)
                        );
                }


                var recordsTotal = query.Count();


                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    var isAscending = sortColumnDirection == "asc";
                    query = sortColumn switch {
                        "DealDate" => isAscending
                            ? query.OrderBy(f => f.DealDateM)
                            : query.OrderByDescending(f => f.DealDateM),
                        "TotalAccAmount" => isAscending
                            ? query.OrderBy(f => f.TotalAccAmount == 0).ThenBy(f => f.TotalAccAmount)
                            : query.OrderBy(f => f.TotalAccAmount == 0).ThenByDescending(f => f.TotalAccAmount),
                        "TotalFactorsAmount" => isAscending
                            ? query.OrderBy(f => f.TotalFactorsAmount == 0).ThenBy(f => f.TotalFactorsAmount)
                            : query.OrderBy(f => f.TotalFactorsAmount == 0).ThenByDescending(f => f.TotalFactorsAmount),
                        "Demand" => isAscending
                            ? query.OrderBy(f => f.Demand)
                            : query.OrderByDescending(f => f.Demand),
                        _ => query
                    };
                }

                var totalAccAmountAll = query.Sum(f => f.TotalAccAmount);
                var totalFactorsAmountAll = query.Sum(f => f.TotalFactorsAmount);
                var demandAll = query.Sum(f => f.Demand);

                if (length != -1)
                    query = query.Skip(start).Take(length);

                var data = query.ToList();

                return Json(new {
                    draw,
                    recordsTotal,
                    recordsFiltered = recordsTotal,
                    data,
                    totals = new {
                        totalAccAmountAll,
                        totalFactorsAmountAll,
                        demandAll
                    }
                });
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetRpMotalebat() // مطالبات
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
                var model = JsonConvert.DeserializeObject<FilterVModel>(Request.Form["model"].FirstOrDefault());
                var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault()?.ToLower().Trim();


                await using var con = new SqlConnection(db.Database.GetDbConnection().ConnectionString);
                var result = (await con.QueryAsync<Rp_Motalebat>(
                    "Rp_Motalebat",
                    new { CusCode = (int?)null },
                    commandType: CommandType.StoredProcedure
                )).AsQueryable();


                if (!string.IsNullOrEmpty(searchValue))
                {
                    result = result.Where(f =>
                        f.CustomerCode == searchValue ||
                        f.Customer.Contains(searchValue)
                    );
                }

                var recordsTotal = result.Count();


                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    var isAscending = sortColumnDirection == "asc";
                    result = sortColumn switch {
                        "PreFactorCount" => isAscending
                            ? result.OrderBy(f => f.PreFactorCount)
                            : result.OrderByDescending(f => f.PreFactorCount),
                        "PreFactorSum" => isAscending
                            ? result.OrderBy(f => f.PreFactorSum)
                            : result.OrderByDescending(f => f.PreFactorSum),
                        "FactorCount" => isAscending
                            ? result.OrderBy(f => f.FactorCount)
                            : result.OrderByDescending(f => f.FactorCount),
                        "FactorSum" => isAscending
                            ? result.OrderBy(f => f.FactorSum)
                            : result.OrderByDescending(f => f.FactorSum),
                        "AccCount" => isAscending
                            ? result.OrderBy(f => f.AccCount)
                            : result.OrderByDescending(f => f.AccCount),
                        "AccSum" => isAscending
                            ? result.OrderBy(f => f.AccSum)
                            : result.OrderByDescending(f => f.AccSum),
                        "RetAccBackCount" => isAscending
                            ? result.OrderBy(f => f.RetAccBackCount)
                            : result.OrderByDescending(f => f.RetAccBackCount),
                        "RetAccBackSum" => isAscending
                            ? result.OrderBy(f => f.RetAccBackSum)
                            : result.OrderByDescending(f => f.RetAccBackSum),
                        "DebitSum" => isAscending
                            ? result.OrderBy(f => f.DebitSum)
                            : result.OrderByDescending(f => f.DebitSum),
                        _ => result
                    };
                }


                if (length != -1)
                    result = result.Skip(start).Take(length);

                var data = result.ToList();

                return Json(new {
                    draw,
                    recordsTotal,
                    recordsFiltered = recordsTotal,
                    data,
                });
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetSuppliesDetails(Guid dealId)
        {
            try
            {
                var pfIds = await db.PreFactors.AsNoTracking().Where(f => f.DealId == dealId).Select(f => f.Id).ToListAsync();
                var supplies = await db.Supplies.AsNoTracking().Where(f => pfIds.Contains(f.PfId)).ToListAsync();

                /*var query = from supply in supplies
                    join pfItem in db.PreFactorItems.AsNoTracking()
                        on supply.PfItemId equals pfItem.Id
                    group new { supply, pfItem } by pfItem.Id
                    into g
                    select new*/


                return PartialView(supplies);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }


        public async Task<IActionResult> GetOrderHeadsBySupplier(Guid supplierId)
        {
            var result =
                from s in db.Supplies.Where(f => f.SupplierId == supplierId)
                join pf in db.PreFactors on s.PfId equals pf.Id
                join oh in db.OrderHeads on pf.Id equals oh.Prefactor
                select oh.Id;
            /*select new
            {
                oh.Id,
                oh.OrderNumber
            };*/

            var data = await result.Distinct().ToListAsync();

            return Ok(data);
        }


        [HttpPost]
        public async Task<IActionResult> ProccessSupplyFactorWithAi([FromBody] Attachment attachment, CancellationToken ct)
        {
            var result = await aiService.ProcessSupplyFactor(attachment, ct);

            if (!result.Success)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }
    }
}