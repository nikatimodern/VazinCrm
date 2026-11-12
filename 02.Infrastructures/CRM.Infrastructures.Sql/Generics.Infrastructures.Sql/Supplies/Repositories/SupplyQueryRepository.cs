using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.BaseItems.Enums;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;
using CRM.Core.Domain.Supplies.Rules;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.Infrastructures.Sql;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Supplies.Repositories;

public sealed class SupplyQueryRepository(CRMContextDb db) : ISupplyQueryRepository {
    public async Task<SupplyResult> GetSupplyFactorAllocationsAsync(Guid id, CancellationToken cancellationToken)
    {
        var factor = await db.SupplyFactors.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.SerialNumber })
            .FirstOrDefaultAsync(cancellationToken);
        if (factor == null)
            return SupplyResult.Missing("فاکتور تامین یافت نشد.");
        var items = await db.SupplyFactorItems.AsNoTracking().Where(x => x.SupplyFactorId == id).OrderBy(x => x.CreateDate).ThenBy(x => x.Id)
            .Select(x => new
                { x.Id, x.Product, x.ProductSmallName, x.WeightCount, x.UnitTxt, Allocated = x.Allocations.Sum(a => (decimal?)a.Weight) ?? 0 })
            .ToListAsync(cancellationToken);
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
                a.Id,
                a.SupplyFactorItemId,
                a.FactorItemId,
                a.WarehouseId,
                a.Weight,
                a.SourceWarehouseProductId,
                SourceWarehouseId = a.SourceWarehouseProduct != null ? (Guid?)a.SourceWarehouseProduct.WarehouseId : null,
                SourceWarehouseTitle = a.SourceWarehouseProduct != null ? a.SourceWarehouseProduct.Warehouse.Title : null,
                SourceWarehouseStock = a.SourceWarehouseProduct != null
                    ? a.SourceWarehouseProduct.Transactions.Where(t => !t.IsDeleted).Sum(t =>
                        t.TransactionType == WarehouseTransactionTypeEnum.ورود ? (decimal?)t.Weight : -t.Weight) ?? 0
                    : 0,
                SourceWarehouseReserved = a.SourceWarehouseProduct != null
                    ? a.SourceWarehouseProduct.WarehouseProductVouchers.Where(v => v.WarehouseVoucher.Status != WarehouseVoucherStatusEnum.تحویل_شده)
                        .Sum(v => (decimal?)v.Weight) ?? 0
                    : 0,
                SourceWarehouseRemainder = db.SupplyFactorItemAllocations
                    .Where(other => a.SourceWarehouseProductId != null && other.SupplyFactorItemId == a.SupplyFactorItemId &&
                                    other.WarehouseId == a.SourceWarehouseProduct.WarehouseId && other.FactorItemId == null)
                    .Sum(other => (decimal?)other.Weight) ?? 0,
                Customer = f != null ? f.Customer : w.Title,
                SerialNumber = f != null ? (int?)f.SerialNumber : null,
                Product = fi != null ? fi.Product : si.Product,
                ProductSmallName = fi != null ? fi.ProductSmallName : si.ProductSmallName,
                FactorItemWeightCount = fi != null ? fi.WeightCount : null,
                FactorItemUnitTxt = fi != null ? fi.UnitTxt : si.UnitTxt,
                WarehouseTitle = w != null ? w.Title : null,
                FactorItemAllocated = db.SupplyFactorItemAllocations.Where(other => a.FactorItemId != null && other.FactorItemId == a.FactorItemId)
                    .Sum(other => (decimal?)other.Weight) ?? 0
            }
        ).ToListAsync(cancellationToken);
        return SupplyResult.Success(new
            { factor, items, allocations, isAllocationComplete = items.Count > 0 && items.All(x => x.Allocated >= x.WeightCount) });
    }

    public async Task<SupplyResult> SearchAllocationFactorItemsAsync(string search, Guid supplyFactorItemId, Guid? allocationId,
        CancellationToken cancellationToken)
    {
        var source = await db.SupplyFactorItems.AsNoTracking()
            .Where(i => i.Id == supplyFactorItemId && db.SupplyFactors.Any(f => f.Id == i.SupplyFactorId)).Select(i => new { i.Unit })
            .FirstOrDefaultAsync(cancellationToken);
        if (source == null)
            return SupplyResult.Missing("آیتم تامین یافت نشد.");
        var unit = (int)source.Unit;
        search = search?.Trim();
        var excludedAllocationId = allocationId ?? Guid.Empty;
        var sourceProductId = await db.SupplyFactorItemAllocations
            .Where(a => a.Id == excludedAllocationId && a.SupplyFactorItemId == supplyFactorItemId).Select(a => a.SourceWarehouseProductId)
            .FirstOrDefaultAsync(cancellationToken);
        var query =
            from i in db.FactorItems.AsNoTracking()
            join f in db.Factors.AsNoTracking() on i.factorId equals f.Id
            where i.Unit == unit
            select new {
                i.Id,
                i.Product,
                i.ProductSmallName,
                i.WeightCount,
                i.UnitTxt,
                f.Customer,
                f.SerialNumber,
                f.Date,
                i.CreateDate,
                i.Row,
                Allocated = db.SupplyFactorItemAllocations.Where(a => a.FactorItemId == i.Id).Sum(a => (decimal?)a.Weight) ?? 0,
                EditingWeight = db.SupplyFactorItemAllocations
                    .Where(a => a.FactorItemId == i.Id && a.Id == excludedAllocationId && a.SupplyFactorItemId == supplyFactorItemId)
                    .Sum(a => (decimal?)a.Weight) ?? 0
            };
        query = query.Where(x => !db.SupplyFactorItemAllocations.Any(a =>
            a.SupplyFactorItemId == supplyFactorItemId && a.FactorItemId == x.Id && a.SourceWarehouseProductId == sourceProductId &&
            a.Id != excludedAllocationId));
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.Customer.Contains(search) || x.Product.Contains(search) || x.ProductSmallName.Contains(search) ||
                x.SerialNumber.ToString().Contains(search));
        var items = await query.OrderByDescending(x => x.CreateDate).ThenByDescending(x => x.Id).Take(201).ToListAsync(cancellationToken);
        var warehouses = await db.Warehouses.AsNoTracking()
            .Where(w => (w.Id == SupplyAllocationRules.WasteId && w.WarehouseType == WarehouseType.انبار_ضایعات) ||
                        (w.Id == SupplyAllocationRules.StorageId && w.WarehouseType == WarehouseType.انبار_ذخیره))
            .Select(w => new { w.Id, w.Title, w.WarehouseType }).ToListAsync(cancellationToken);
        return SupplyResult.Success(new {
            warehouses,
            items = items.Take(200).Select(i => new {
                i.Id, i.Product, i.ProductSmallName, i.WeightCount, i.UnitTxt, i.Customer, i.SerialNumber, i.Date, i.Allocated,
                Remaining = SupplyAllocationRules.SalesCapacity(i.WeightCount, i.Allocated),
                Available = SupplyAllocationRules.SalesCapacity(i.WeightCount, i.Allocated, i.EditingWeight)
            }),
            hasMore = items.Count > 200
        });
    }

    public async Task<SupplyResult> GetSalesFactorAllocationsAsync(Guid id, CancellationToken cancellationToken)
    {
        var factor = await db.Factors.AsNoTracking().Where(f => f.Id == id).Select(f => new { f.Id, f.SerialNumber })
            .FirstOrDefaultAsync(cancellationToken);
        if (factor == null)
            return SupplyResult.Missing("فاکتور فروش یافت نشد.");
        var rows = await db.FactorItems.AsNoTracking().Where(i => i.factorId == id).OrderBy(i => i.Row).ThenBy(i => i.CreateDate).ThenBy(i => i.Id)
            .Select(i => new
                { i.Id, i.Product, i.ProductSmallName, i.WeightCount, i.UnitTxt, Allocated = i.Allocations.Sum(a => (decimal?)a.Weight) ?? 0 })
            .ToListAsync(cancellationToken);
        var items = rows.Select(i => new {
            i.Id, i.Product, i.ProductSmallName, i.UnitTxt, i.Allocated,
            WeightCount = SupplyAllocationRules.TryParseSalesWeight(i.WeightCount, out var weight) ? Math.Max(0, weight) : 0
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
                a.Id,
                a.SupplyFactorItemId,
                a.FactorItemId,
                a.WarehouseId,
                a.Weight,
                a.SourceWarehouseProductId,
                SourceWarehouseId = a.SourceWarehouseProduct != null ? (Guid?)a.SourceWarehouseProduct.WarehouseId : null,
                SourceWarehouseTitle = a.SourceWarehouseProduct != null ? a.SourceWarehouseProduct.Warehouse.Title : null,
                SourceWarehouseStock = a.SourceWarehouseProduct != null
                    ? a.SourceWarehouseProduct.Transactions.Where(t => !t.IsDeleted).Sum(t =>
                        t.TransactionType == WarehouseTransactionTypeEnum.ورود ? (decimal?)t.Weight : -t.Weight) ?? 0
                    : 0,
                SourceWarehouseReserved = a.SourceWarehouseProduct != null
                    ? a.SourceWarehouseProduct.WarehouseProductVouchers.Where(v => v.WarehouseVoucher.Status != WarehouseVoucherStatusEnum.تحویل_شده)
                        .Sum(v => (decimal?)v.Weight) ?? 0
                    : 0,
                SourceWarehouseRemainder = db.SupplyFactorItemAllocations
                    .Where(other => a.SourceWarehouseProductId != null && other.SupplyFactorItemId == a.SupplyFactorItemId &&
                                    other.WarehouseId == a.SourceWarehouseProduct.WarehouseId && other.FactorItemId == null)
                    .Sum(other => (decimal?)other.Weight) ?? 0,
                Supplier = sf != null ? sf.SupplierTxt : null,
                SerialNumber = sf != null ? sf.SerialNumber : null,
                Date = sf != null ? sf.Date : null,
                Product = si != null ? si.Product : fi.Product,
                ProductSmallName = si != null ? si.ProductSmallName : fi.ProductSmallName,
                SupplyItemWeightCount = si != null ? (decimal?)si.WeightCount : null,
                SupplyItemUnitTxt = si != null ? si.UnitTxt : fi.UnitTxt,
                WarehouseTitle = w != null ? w.Title : null,
                SupplyItemAllocated = db.SupplyFactorItemAllocations
                    .Where(other => a.SupplyFactorItemId != null && other.SupplyFactorItemId == a.SupplyFactorItemId)
                    .Sum(other => (decimal?)other.Weight) ?? 0
            }
        ).ToListAsync(cancellationToken);
        return SupplyResult.Success(new
            { factor, items, allocations, isAllocationComplete = items.Count > 0 && items.All(i => i.Allocated >= i.WeightCount) });
    }

    public async Task<SupplyResult> SearchAllocationSupplyItemsAsync(string search, Guid factorItemId, Guid? allocationId,
        CancellationToken cancellationToken)
    {
        var sales = await db.FactorItems.AsNoTracking().Where(i => i.Id == factorItemId && db.Factors.Any(f => f.Id == i.factorId))
            .Select(i => new { i.Unit }).FirstOrDefaultAsync(cancellationToken);
        if (sales == null)
            return SupplyResult.Missing("آیتم فروش یافت نشد.");
        var unit = (UnitTypeEnums)sales.Unit;
        search = search?.Trim();
        var excludedId = allocationId ?? Guid.Empty;
        var query =
            from i in db.SupplyFactorItems.AsNoTracking()
            join f in db.SupplyFactors.AsNoTracking() on i.SupplyFactorId equals f.Id
            where i.Unit == unit && !db.SupplyFactorItemAllocations.Any(a =>
                a.FactorItemId == factorItemId && a.SupplyFactorItemId == i.Id && a.SourceWarehouseProductId == null && a.Id != excludedId)
            select new {
                i.Id,
                i.Product,
                i.ProductSmallName,
                i.WeightCount,
                i.UnitTxt,
                Supplier = f.SupplierTxt,
                f.SerialNumber,
                f.Date,
                i.CreateDate,
                Allocated = i.Allocations.Sum(a => (decimal?)a.Weight) ?? 0,
                EditingWeight = i.Allocations.Where(a => a.Id == excludedId && a.FactorItemId == factorItemId && a.SourceWarehouseProductId == null)
                    .Sum(a => (decimal?)a.Weight) ?? 0
            };
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i =>
                i.Supplier.Contains(search) || i.SerialNumber.Contains(search) || i.Product.Contains(search) || i.ProductSmallName.Contains(search));
        var items = await query.OrderByDescending(i => i.CreateDate).ThenByDescending(i => i.Id).Take(201).ToListAsync(cancellationToken);
        var warehouses = await db.Warehouses.AsNoTracking()
            .Where(w => (w.Id == SupplyAllocationRules.WasteId && w.WarehouseType == WarehouseType.انبار_ضایعات) ||
                        (w.Id == SupplyAllocationRules.StorageId && w.WarehouseType == WarehouseType.انبار_ذخیره))
            .Select(w => new { w.Id, w.Title, w.WarehouseType }).ToListAsync(cancellationToken);
        var inventory =
            from p in db.WarehouseProducts.AsNoTracking()
            join si in db.SupplyFactorItems on p.SupplyFactorItemId equals si.Id
            join sf in db.SupplyFactors on si.SupplyFactorId equals sf.Id
            join w in db.Warehouses on p.WarehouseId equals w.Id
            where p.Unit == unit && si.Unit == unit &&
                  ((w.Id == SupplyAllocationRules.WasteId && w.WarehouseType == WarehouseType.انبار_ضایعات) ||
                   (w.Id == SupplyAllocationRules.StorageId && w.WarehouseType == WarehouseType.انبار_ذخیره)) &&
                  !db.SupplyFactorItemAllocations.Any(a => a.SourceWarehouseProductId == p.Id && a.FactorItemId == factorItemId && a.Id != excludedId)
            select new {
                p.Id,
                p.WarehouseId,
                WarehouseTitle = w.Title,
                p.SupplyFactorItemId,
                Customer = sf.SupplierTxt,
                sf.SerialNumber,
                sf.Date,
                Product = p.ProductDescription,
                p.ProductSmallName,
                p.UnitTxt,
                p.CreateDate,
                Stock = p.Transactions.Sum(t => t.TransactionType == WarehouseTransactionTypeEnum.ورود ? (decimal?)t.Weight : -t.Weight) ?? 0,
                Reserved = p.WarehouseProductVouchers.Where(v => v.WarehouseVoucher.Status != WarehouseVoucherStatusEnum.تحویل_شده)
                    .Sum(v => (decimal?)v.Weight) ?? 0,
                Remainder = db.SupplyFactorItemAllocations
                    .Where(a => a.SupplyFactorItemId == si.Id && a.WarehouseId == w.Id && a.FactorItemId == null).Sum(a => (decimal?)a.Weight) ?? 0,
                Allocated = db.SupplyFactorItemAllocations.Where(a => a.SourceWarehouseProductId == p.Id).Sum(a => (decimal?)a.Weight) ?? 0,
                EditingWeight = db.SupplyFactorItemAllocations
                    .Where(a => a.Id == excludedId && a.FactorItemId == factorItemId && a.SourceWarehouseProductId == p.Id)
                    .Sum(a => (decimal?)a.Weight) ?? 0
            };
        if (!string.IsNullOrWhiteSpace(search))
            inventory = inventory.Where(i =>
                i.Customer.Contains(search) || i.SerialNumber.Contains(search) || i.Product.Contains(search) || i.ProductSmallName.Contains(search) ||
                i.WarehouseTitle.Contains(search));
        var stockItems = await inventory.OrderByDescending(i => i.CreateDate).ThenByDescending(i => i.Id).Take(201).ToListAsync(cancellationToken);
        return SupplyResult.Success(new {
            warehouses, hasMore = items.Count > 200, warehouseHasMore = stockItems.Count > 200,
            warehouseItems = stockItems.Take(200).Select(i => new {
                i.Id, i.WarehouseId, i.WarehouseTitle, i.SupplyFactorItemId, i.Customer, i.SerialNumber, i.Date, i.Product, i.ProductSmallName,
                i.UnitTxt, WeightCount = i.Stock, i.Allocated, Remaining = Math.Max(0, Math.Min(i.Stock - i.Reserved, i.Remainder)),
                Available = Math.Max(0, Math.Min(i.Stock - i.Reserved + i.EditingWeight, i.Remainder + i.EditingWeight))
            }),
            items = items.Take(200).Select(i => new {
                i.Id, i.Product, i.ProductSmallName, i.WeightCount, i.UnitTxt, Customer = i.Supplier, i.SerialNumber, i.Date, i.Allocated,
                Remaining = Math.Max(0, i.WeightCount - i.Allocated), Available = Math.Max(0, i.WeightCount - i.Allocated + i.EditingWeight)
            })
        });
    }

    public async Task<SupplyResult> GetPaymentAsync(Guid? paymentId, CancellationToken cancellationToken)
    {
        try
        {
            var query = db.Payments.AsNoTracking().Where(f => f.Id == paymentId);
            var data = await query.Include(f => f.PaymentSupplyFactors).AsNoTracking().Select(p => new {
                Payment = p,
                SupplyFactorSerials = p.PaymentSupplyFactors
                    .Join(db.SupplyFactors, pSf => pSf.SupplyFactorId, sf => sf.Id, (pSf, sf) => sf.SerialNumber).ToList(),
                Attachments = db.Attachments.Where(a => a.EntityId == p.Id && a._AttachType == _AttachType.Payment).ToList()
            }).FirstOrDefaultAsync(cancellationToken);
            return SupplyResult.Success(data);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SupplyResult.Invalid();
        }
    }

    public async Task<SupplyResult> GetSupplyPaymentsAsync(SupplyTableRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var draw = request.Draw;
            var start = request.Start;
            var length = request.Length;
            var sortColumn = request.SortColumn;
            var sortColumnDirection = request.SortDirection;
            var searchValue = request.Search;
            searchValue = searchValue?.ToLower().Trim();
            var query = db.Payments.OrderByDescending(f => f.CreateDate).AsNoTracking();
            if (!string.IsNullOrEmpty(searchValue))
            {
                var searchNumber = SearchNumber(searchValue);
                query = query.Where(f =>
                    f.Amount == searchNumber || f.PaymentNumber == searchNumber || f.FromAccountTxt.Contains(searchValue) ||
                    f.ToAccountTxt.Contains(searchValue));
            }

            var recordsTotal = await query.CountAsync(cancellationToken);
            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
            {
                var isAscending = sortColumnDirection == "asc";
                query = sortColumn switch {
                    "PaymentNumber" => isAscending ? query.OrderBy(f => f.PaymentNumber) : query.OrderByDescending(f => f.PaymentNumber),
                    "Amount" => isAscending ? query.OrderBy(f => f.Amount) : query.OrderByDescending(f => f.Amount),
                    "CreateDateTxt" => isAscending ? query.OrderBy(f => f.CreateDate) : query.OrderByDescending(f => f.CreateDate),
                    "PaymentDateTxt" => isAscending ? query.OrderBy(f => f.PaymentDate) : query.OrderByDescending(f => f.PaymentDate),
                    _ => query
                };
            }

            if (length != -1)
                query = query.Skip(start).Take(length);
            var data = await query.ToListAsync(cancellationToken);
            return SupplyResult.Success(new { draw, recordsTotal, recordsFiltered = recordsTotal, data, });
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SupplyResult.Invalid();
        }
    }

    public async Task<SupplyResult> GetSupplierFinancialSummaryAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        var summary = await GetSupplierFinancialSummaries().FirstOrDefaultAsync(x => x.SupplierId == supplierId, cancellationToken);
        return SupplyResult.Success(summary);
    }

    public async Task<SupplyResult> GetSupplyFactorsAsync(string search, Guid? dealId, CancellationToken cancellationToken)
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
                var searchNumber = SearchNumber(search);
                query = query.Where(f =>
                    f.SerialNumber == search || f.SupplierTxt.Contains(search) || f.SupplyFactorOrderHeads.Any(sfOh =>
                        db.OrderHeads.Any(oh => oh.Id == sfOh.OrderHeadId && oh.OrderNumber == searchNumber)));
            }

            var data = await query.OrderByDescending(f => f.CreateDate).Select(sf => new SupplyFactorVModel {
                IsAllocationComplete = sf.Items.Any() && sf.Items.All(i => (i.Allocations.Sum(a => (decimal?)a.Weight) ?? 0) >= i.WeightCount),
                SupplyFactor = sf,
                OrderSerials = sf.SupplyFactorOrderHeads.Join(db.OrderHeads, sfOh => sfOh.OrderHeadId, oh => oh.Id, (sfOh, oh) => oh.OrderNumber)
                    .ToList()
            }).ToListAsync(cancellationToken);
            return SupplyResult.Success(data);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SupplyResult.Invalid();
        }
    }

    public async Task<SupplyResult> SearchSupplyFactorsAsync(SupplySelection model, CancellationToken cancellationToken)
    {
        try
        {
            var search = model.Search;
            var searchNumber = SearchNumber(search);
            var query = db.SupplyFactors.OrderByDescending(f => model.SelectedIds.Contains(f.Id)).ThenByDescending(f => f.CreateDate).AsNoTracking()
                .AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(f => f.SupplierTxt.Contains(search) || f.SerialNumber == search || f.PaymentAmount == searchNumber);
            } else
            {
                query = query.Take(50);
            }

            var data = await query.ToListAsync(cancellationToken);
            return SupplyResult.Success(data);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SupplyResult.Invalid();
        }
    }

    public async Task<SupplyResult> GetSupplyFactorAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var data = await db.SupplyFactors.AsNoTracking().Where(f => f.Id == id).Include(f => f.SupplyFactorOrderHeads).Include(f => f.Items)
                .Select(sf => new {
                    SupplyFactor = sf,
                    OrderSerials = sf.SupplyFactorOrderHeads.Join(db.OrderHeads, sfo => sfo.OrderHeadId, oh => oh.Id, (sfo, oh) => oh.OrderNumber)
                        .ToList(),
                    Attachments = db.Attachments.Where(a => a.EntityId == sf.Id && a._AttachType == _AttachType.SupplyFactor).ToList()
                }).FirstOrDefaultAsync(cancellationToken);
            return SupplyResult.Success(data);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SupplyResult.Invalid();
        }
    }

    public async Task<SupplyResult> GetSupplyBaseDataAsync(CancellationToken cancellationToken)
    {
        var extraBaseItems = await db.BaseItems.Where(f => f.TypeCode == BaseItemEnum.هزینه_اضافی_تولید)
            .Select(f => new {
                Value = f.Id,
                Text = f.Title
            }).ToListAsync(cancellationToken);
        
        var suppliers = await db.Customers.Where(f => f.RoleType == RoleTypeEnum.تامین_کننده || f.RoleType == RoleTypeEnum.تامین_کننده_مشتری)
            .OrderByDescending(f => f.CreateDate)
            .Select(f => new {
                Value = f.Id,
                Text = $"{f.CompanyName ?? ""} {f.AgentName ?? ""} ({f.CustomerCode})".Trim(),
            })
            .ToListAsync(cancellationToken);
        
        var model = new {
            extraBaseItems,
            suppliers
        };
        return SupplyResult.Success(model);
    }

    public async Task<SupplyResult> GetSupplierOrdersAsync(CancellationToken cancellationToken)
    {
        try
        {
            var rows = await (
                from o in db.Orders.AsNoTracking()
                join oh in db.OrderHeads.AsNoTracking() on o.OrderHeadId equals oh.Id
                where oh.Status != OrderHeadStatusEnum.تحویل_شده && oh.SupplierId != null
                select new {
                    o,
                    oh
                }
            ).ToListAsync(cancellationToken);
            var result = rows.GroupBy(x => new { x.oh.SupplierId, x.oh.SupplierTxt }).Select(g => new GroupedOrdersBySupplierVModal {
                SupplierId = g.Key.SupplierId, SupplierTxt = g.Key.SupplierTxt,
                Products = g.GroupBy(p => p.o.ProductCode).Select(x => new ProductGroupVModel {
                    Orders = x.Select(z => z.o).ToList(), OrderHead = x.Select(z => z.oh).FirstOrDefault(),
                    SumWeight = x.Sum(f => int.TryParse(f.o.Weight, out var w) ? w : 0),
                    ProductSmallName = x.Select(g => g.o.ProductSmallName).FirstOrDefault(), UnitTxt = x.Select(g => g.o.UnitTxt).FirstOrDefault(),
                }).ToList()
            }).ToList();
            return SupplyResult.Success(result);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SupplyResult.Invalid();
        }
    }

    public async Task<SupplyResult> GetSuppliesAsync(Guid pfId, CancellationToken cancellationToken)
    {
        var data = await db.Supplies.AsNoTracking().Where(f => f.PfId == pfId).ToListAsync(cancellationToken);
        return SupplyResult.Success(data);
    }

    public async Task<SupplyResult> GetSupplyProfitAsync(SupplyTableRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var draw = request.Draw;
            var start = request.Start;
            var length = request.Length;
            var sortColumn = request.SortColumn;
            var sortColumnDirection = request.SortDirection;
            var searchValue = request.Search;
            var fromDate = request.FromDate;
            var toDate = request.ToDate;
            var userCreatorDealId = request.UserId;
            var dateFilterChk = request.DateFilterEnabled;
            var userFilterChk = request.UserFilterEnabled;
            var procedureParameters = new {
                FromDate = fromDate,
                ToDate = toDate,
                DateFilterChk = dateFilterChk,
                UserCreatorDealId = userCreatorDealId,
                SelFilterChd = userFilterChk,
                Type = request.Type,
                SearchValue = string.IsNullOrWhiteSpace(searchValue) ? null : searchValue,
                Start = start,
                Length = length,
                SortColumn = string.IsNullOrWhiteSpace(sortColumn) ? "DealDate" : sortColumn,
                SortDirection = string.IsNullOrWhiteSpace(sortColumnDirection) ? "desc" : sortColumnDirection
            };
            await using var connection = new SqlConnection(db.Database.GetDbConnection().ConnectionString);
            using var multi = await connection.QueryMultipleAsync(new CommandDefinition("dbo.Sp_GetSuppliesProfit", procedureParameters,
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
            var data = (await multi.ReadAsync<SuppliesProfitDtVModel>()).ToList();
            var totalsRow = await multi.ReadSingleAsync();
            var recordsFiltered = Convert.ToInt64(totalsRow.RecordsFiltered);
            return SupplyResult.Success(new {
                draw, recordsTotal = recordsFiltered, recordsFiltered, data,
                totals = new {
                    totalSuppliesAmountAll = Convert.ToDecimal(totalsRow.TotalSuppliesAmountAll),
                    totalFactorsAmountAll = Convert.ToDecimal(totalsRow.TotalFactorsAmountAll), profitAll = Convert.ToDecimal(totalsRow.ProfitAll)
                }
            });
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // logger.LogError(ex, "Error in GetSuppliesProfitDt");
            return SupplyResult.Invalid();
        }
    }

    public async Task<SupplyResult> GetSupplyDemandsAsync(SupplyTableRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var draw = request.Draw;
            var start = request.Start;
            var length = request.Length;
            var sortColumn = request.SortColumn;
            var sortColumnDirection = request.SortDirection;
            var searchValue = request.Search;
            searchValue = searchValue?.ToLower().Trim();
            
            var factorQuery = db.PreFactors
                .AsNoTracking()
                .Where(pf =>
                    pf.Pftype != (int)PreFactorEnum.فاقد_اعتبار &&
                    pf.Pftype != (int)PreFactorEnum.بسته_شده &&
                    pf.Factors.Any(f => f.status == FactorStatus.تایید_شده)).GroupBy(pf => pf.DealId).Select(g => new {
                    DealId = g.Key,
                    TotalFactorsAmount = g.SelectMany(x => x.Factors)
                        .Where(f => f.status == FactorStatus.تایید_شده)
                        .Sum(f => f.ParentReturnedId == null ? f.PaymentAmount : -f.PaymentAmount)
                });

            var accQuery = db.Accountancies
                .AsNoTracking()
                .Where(acc =>
                    acc.InvoiceType == InvoiceTypeEnum.پیش_فاکتور &&
                    (acc.AccType == AcEnum.واریزی || acc.AccType == AcEnum.برگشتی) &&
                    (acc.PayType == PayTypeEnum.نقدی || acc.PayType == PayTypeEnum.تهاتری) &&
                    acc.Status == AccStatusEnum.تایید_شده
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
            if (request.DateFilterEnabled)
                query = query.Where(f => f.DealDateM >= request.FromDate && f.DealDateM <= request.ToDate);
            if (request.UserFilterEnabled)
                query = query.Where(f => f.UserCreatorDealId == request.UserId);
            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(f => f.DealSerialNumber.ToString() == searchValue || f.CustomerName.Contains(searchValue));
            }

            var recordsTotal = await query.CountAsync(cancellationToken);
            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
            {
                var isAscending = sortColumnDirection == "asc";
                query = sortColumn switch {
                    "DealDate" => isAscending ? query.OrderBy(f => f.DealDateM) : query.OrderByDescending(f => f.DealDateM),
                    "TotalAccAmount" => isAscending
                        ? query.OrderBy(f => f.TotalAccAmount == 0).ThenBy(f => f.TotalAccAmount)
                        : query.OrderBy(f => f.TotalAccAmount == 0).ThenByDescending(f => f.TotalAccAmount),
                    "TotalFactorsAmount" => isAscending
                        ? query.OrderBy(f => f.TotalFactorsAmount == 0).ThenBy(f => f.TotalFactorsAmount)
                        : query.OrderBy(f => f.TotalFactorsAmount == 0).ThenByDescending(f => f.TotalFactorsAmount),
                    "Demand" => isAscending ? query.OrderBy(f => f.Demand) : query.OrderByDescending(f => f.Demand),
                    _ => query
                };
            }

            var totalAccAmountAll = await query.SumAsync(f => f.TotalAccAmount, cancellationToken);
            var totalFactorsAmountAll = await query.SumAsync(f => f.TotalFactorsAmount, cancellationToken);
            var demandAll = await query.SumAsync(f => f.Demand, cancellationToken);
            if (length != -1)
                query = query.Skip(start).Take(length);
            var data = await query.ToListAsync(cancellationToken);
            return SupplyResult.Success(new
                { draw, recordsTotal, recordsFiltered = recordsTotal, data, totals = new { totalAccAmountAll, totalFactorsAmountAll, demandAll } });
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SupplyResult.Invalid();
        }
    }


    public async Task<SupplyResult> GetReceivablesAsync(
        SupplyTableRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            int? customerCode = null;

            // تبدیل شناسه مشتری به کد مشتری
            if (!string.IsNullOrWhiteSpace(request.CustomerId))
            {
                if (!Guid.TryParse(request.CustomerId, out var customerId))
                    return SupplyResult.Invalid();

                customerCode = await db.Customers
                    .AsNoTracking()
                    .Where(c => c.Id == customerId && !c.IsDeleted)
                    .Select(c => (int?)c.CustomerCode)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!customerCode.HasValue)
                    return SupplyResult.Invalid();
            }

            var parameters = new
            {
                CusCode = customerCode,
                Start = request.Start,
                Length = request.Length,
                Search = request.Search?.Trim(),
                SortColumn = request.SortColumn,
                SortDirection = request.SortDirection,
                DateFilterEnabled = request.DateFilterEnabled,
                FromDate = request.FromDate,
                ToDate = request.ToDate,

                CashPayType = (int)PayTypeEnum.نقدی,
                CheckPayType = (int)PayTypeEnum.چکی,
                BarterPayType = (int)PayTypeEnum.تهاتری
            };

            var con = db.Database.GetDbConnection();

            using var result = await con.QueryMultipleAsync(
                new CommandDefinition(
                    "dbo.Rp_Motalebat",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken
                )
            );

            // خروجی اول: مشتریان صفحه جاری
            var data = (await result.ReadAsync<Rp_Motalebat>())
                .ToList();

            // خروجی دوم: تعداد رکوردها و مجموع مبالغ
            var summary = await result.ReadFirstAsync<ReceivablesSummary>();

            var totals = new
            {
                factorCount = summary.TotalFactorCount,
                factorSum = summary.TotalFactorSum,

                accCount = summary.TotalAccCount,
                accSum = summary.TotalAccSum,

                retAccBackCount = summary.TotalRetAccBackCount,
                retAccBackSum = summary.TotalRetAccBackSum,

                debitSum = summary.TotalDebitSum,
                receivableSum = summary.TotalReceivableSum,
                debtSum = summary.TotalDebtSum
            };

            return SupplyResult.Success(new
            {
                draw = request.Draw,
                recordsTotal = summary.RecordsTotal,
                recordsFiltered = summary.RecordsFiltered,
                data,
                totals
            });
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SupplyResult.Invalid();
        }
    }


    public async Task<SupplyResult> GetDealSuppliesAsync(Guid dealId, CancellationToken cancellationToken)
    {
        try
        {
            var pfIds = await db.PreFactors.AsNoTracking().Where(f => f.DealId == dealId).Select(f => f.Id).ToListAsync(cancellationToken);
            var supplies = await db.Supplies.AsNoTracking().Where(f => pfIds.Contains(f.PfId)).ToListAsync(cancellationToken);
            return SupplyResult.Success(supplies);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SupplyResult.Invalid();
        }
    }

    public async Task<SupplyResult> GetOrderHeadsBySupplierAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        var result =
            from s in db.Supplies.Where(f => f.SupplierId == supplierId)
            join pf in db.PreFactors on s.PfId equals pf.Id
            join oh in db.OrderHeads on pf.Id equals oh.Prefactor
            select oh.Id;
        var data = await result.Distinct().ToListAsync(cancellationToken);
        return SupplyResult.Success(data);
    }

    private static int SearchNumber(string value)
    {
        try
        {
            return Convert.ToInt32(value);
        }
        catch (FormatException)
        {
            return 0;
        }
        catch (OverflowException)
        {
            return 0;
        }
    }

    private IQueryable<SupplierFinancialSummaryVModel> GetSupplierFinancialSummaries()
    {
        var supplyAmounts = db.SupplyFactors
            .AsNoTracking()
            .GroupBy(x => x.SupplierId)
            .Select(g => new {
                SupplierId = g.Key,
                SupplyAmount = g.Sum(x => x.PaymentAmount),
                PaidAmount = 0m
            });

        var paidAmounts = db.RequestAmountPayments
            .AsNoTracking()
            .Where(x => x.RequestAmount.SupplierId.HasValue)
            .GroupBy(x => x.RequestAmount.SupplierId.Value)
            .Select(g => new {
                SupplierId = g.Key,
                SupplyAmount = 0m,
                PaidAmount = g.Sum(x => x.Amount)
            });

        return supplyAmounts
            .Concat(paidAmounts)
            .GroupBy(x => x.SupplierId)
            .Select(g => new SupplierFinancialSummaryVModel {
                    SupplierId = g.Key,
                    TotalSupplyAmount = g.Sum(x => x.SupplyAmount),
                    TotalPaidAmount = g.Sum(x => x.PaidAmount),
                    SupplierDebt = g.Sum(x => x.SupplyAmount) - g.Sum(x => x.PaidAmount)
                }
            );
    }
}
