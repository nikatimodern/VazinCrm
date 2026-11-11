#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Cartables.Repositories;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Querying;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Supplies.Enums;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;
using CRM.Infrastructures.Sql.Querying;
using Microsoft.EntityFrameworkCore;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.ASettings.Enums;

namespace CRM.Infrastructures.Sql.Cartables.Repositories;
public sealed class CartableRepository(CRMContextDb db) : ICartableRepository
{
    public Task<ASetting?> GetSettingsAsync(CancellationToken ct) => db.ASettings.AsNoTracking().FirstOrDefaultAsync(ct);
    public Task<int> GetUnreadChatsAsync(string userId, CancellationToken ct) => db.MainChats.Where(f => f.ToUserId == userId || f.FromUserId == userId).SumAsync(f => f.Chats.Count(c => !c.Read && c.UserCreatorID != userId), ct);
    public Task<List<PreFactor>> GetOwnPreFactorsAsync(string userId, CancellationToken ct) => db.PreFactors.AsNoTracking().Where(f => f.UserCreatorID == userId).ToListAsync(ct);
    public Task<List<Factor>?> GetModalFactorsAsync(Guid id, int type, CancellationToken ct) => type switch
    {
        4 => Read(db.Factors.Where(f => f.DealId == id), ct),
        5 => Read(db.Factors.Where(f => f.PfactorID == id), ct),
        6 => Read(db.Factors.Where(f => f.Id == id), ct),
        7 => Read(db.Factors.Where(f => f.CustomerId == id && f.status == FactorStatus.تایید_شده), ct),
        _ => Task.FromResult<List<Factor>?>(null)};
    private static async Task<List<Factor>?> Read(IQueryable<Factor> query, CancellationToken ct) => await query.AsNoTracking().ToListAsync(ct);
    public async Task<IList<PreFactor>> GetPreFactorsAsync(QueryFilter filter, CartableReadScope scope, int searchNumber, CancellationToken ct)
    {
        var pListQ = db.PreFactors.Include(p => p.Deal).Include(p => p.CustomerEntity).OrderByDescending(p => p.DateMiladi).ThenByDescending(p => p.CreateDate).AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var ser = filter.Search;
            int? serNum = searchNumber;
            pListQ = pListQ.Where(s => EF.Functions.Like(s.AgentName, "%" + ser + "%") || EF.Functions.Like(s.Customer, "%" + ser + "%") || s.SerialNumber == serNum || s.CustomerEntity.CustomerCode == serNum);
        }

        if (filter.HasFilter)
        {
            pListQ = pListQ.ApplyFilter(filter, f => f.CustomerId, f => f.DateMiladi, f => f.UserCreatorID);
        }

        if (!scope.ReadAll)
        {
            pListQ = pListQ.Where(f => f.UserCreatorID == scope.UserId);
        }

        pListQ = filter.Type switch
        {
            5 => pListQ.Where(f => !f.Factors.Any()), // بدون فاکتور
            6 => pListQ.Where(f => f.Factors.Any()), // دارای فاکتور
            _ => pListQ
        };
        if (!filter.HasFilter)
        {
            pListQ = pListQ.Take(50);
        }

        var pList = await pListQ.AsNoTracking().ToListAsync(ct);
        return pList;
    }

    public async Task<IList<Factor>> GetFactorsAsync(QueryFilter filter, Guid? customerId, CartableReadScope scope, int searchNumber, CancellationToken ct)
    {
        var fListQ = db.Factors.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var ser = filter.Search;
            int? serNum = searchNumber;
            fListQ = fListQ.Where(s => EF.Functions.Like(s.AgentName, "%" + ser + "%") || EF.Functions.Like(s.Customer, "%" + ser + "%") || s.SerialNumber == serNum || s.CustomerCode == serNum || s.SnFinancial == ser);
        }

        if (!scope.ReadAll)
        {
            fListQ = fListQ.Where(f => f.SelerId == scope.UserId);
        }

        if (filter.HasFilter)
        {
            fListQ = fListQ.ApplyFilter(filter, f => f.CustomerId, f => f.DateMiladi, f => f.SelerId);
        }

        fListQ = filter.Type switch
        {
            1 => fListQ.Where(f => f.OfficialType == 1),
            2 => fListQ.Where(f => f.OfficialType == 0),
            3 => fListQ.Where(f => f.ParentReturnedId != null),
            _ => fListQ
        };
        if (!string.IsNullOrEmpty(filter.EntityId))
        {
            fListQ = fListQ.Where(f => f.PfactorID == Guid.Parse(filter.EntityId)).AsQueryable();
        }
        else if (customerId != null)
        {
            fListQ = fListQ.Where(f => f.CustomerId == customerId).AsQueryable();
        }

        fListQ = fListQ.OrderByDescending(f => f.DateMiladi).ThenByDescending(f => f.CreateDate);
        if (!filter.HasFilter)
        {
            fListQ = fListQ.Take(50);
        }

        var fList = await fListQ.AsNoTracking().ToListAsync(ct);
        await FillExternalFinancialMappingStateAsync(fList, ct);
        return fList;
    }

    public async Task<CartableSupplyData> GetSupplyAsync(string? search, int type, CartableReadScope scope, int searchNumber, CancellationToken ct)
    {
        var queryBase = db.PreFactors.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var serialVal = searchNumber;
            queryBase = queryBase.Where(s => EF.Functions.Like(s.AgentName, $"%{search}%") || EF.Functions.Like(s.Customer, $"%{search}%") || s.SerialNumber == serialVal);
        }
        else if (!scope.SupplyAdmin)
        {
            queryBase = queryBase.Where(f => f.UserTaminId == Guid.Parse(scope.UserId));
        }

        var filterCounts = new Dictionary<int, int>
        {
            [-1] = await queryBase.CountAsync(s => s.Pftype >= (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین, ct),
            [1] = await queryBase.CountAsync(s => s.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین, ct),
            [2] = await queryBase.CountAsync(s => s.Pftype > (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین, ct),
        };
        var query = type switch
        {
            -1 => queryBase.Where(s => s.Pftype >= (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین),
            1 => queryBase.Where(s => s.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین),
            2 => queryBase.Where(s => s.Pftype > (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین),
            _ => queryBase
        };
        var preFactors = await query.OrderByDescending(f => f.ModifyDate).Take(100).Include(f => f.PreFactorItems).ToListAsync(ct);
        var pfIds = preFactors.Select(f => f.Id).ToList();
        var supplies = await db.Supplies.Where(f => pfIds.Contains(f.PfId)).GroupBy(f => f.PfId).Select(f => new { pfId = f.Key, totalAmount = f.Sum(g => g.TotalAmount), //totalWeight = f.Where(g => g.SupplyType == SupplyTypeEnum.هزینه_تولید).Sum(g => Convert.ToDecimal(g.Weight)),
        totalItems = f.Where(g => g.SupplyType == SupplyTypeEnum.هزینه_تولید).GroupBy(g => g.PfItemId).Count() }).ToListAsync(ct);
        foreach (var pf in preFactors)
        {
            var s = supplies.FirstOrDefault(f => f.pfId == pf.Id);
            if (s == null)
            {
                pf.NoSupply = true;
                continue;
            }

            //var pfItemsWeight = pf.PreFactorItems.Sum(x => Convert.ToDecimal(x?.WeightCount));
            var pfItemsCount = pf.PreFactorItems.Count;
            pf.TotalSupplyAmount = s.totalAmount;
            pf.SupplyMismatch = s.totalItems != pfItemsCount; //|| s.totalWeight != pfItemsWeight;
        }

        return new(preFactors, filterCounts);
    }

    public async Task<CartableFinancialData> GetFinancialAsync(string? search, int type, int searchNumber, CancellationToken ct)
    {
        var baseQuery = db.PreFactors.OrderByDescending(g => g.ModifyDate).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            baseQuery = baseQuery.Where(f => f.AgentName.Contains(search) || f.Customer.Contains(search) || f.SerialNumber == searchNumber);
        }

        const int pending = (int)PreFactorEnum.در_انتظار_تایید_مالی;
        int[] approved = [(int)PreFactorEnum.در_انتظار_تولید, (int)PreFactorEnum.جنس_به_مشتری_تحویل_داده_شد];
        var filterCounts = new Dictionary<int, int>
        {
            [-1] = await baseQuery.Where(s => approved.Contains(s.Pftype) || s.Pftype == pending || s.AcceptedFinancial != null).CountAsync(ct),
            [1] = await baseQuery.Where(s => s.Pftype == pending).CountAsync(ct),
            [2] = await baseQuery.Where(s => approved.Contains(s.Pftype) || s.AcceptedFinancial == true).CountAsync(ct),
            [3] = await baseQuery.Where(s => s.AcceptedFinancial == false).CountAsync(ct),
        };
        var query = type switch
        {
            -1 => baseQuery.Where(f => approved.Contains(f.Pftype) || f.Pftype == pending || f.AcceptedFinancial != null),
            1 => baseQuery.Where(f => f.Pftype == pending),
            2 => baseQuery.Where(f => approved.Contains(f.Pftype) || f.AcceptedFinancial == true),
            3 => baseQuery.Where(f => f.AcceptedFinancial == false),
            _ => baseQuery
        };
        query = string.IsNullOrEmpty(search) ? query.Take(100) : query;
        var result = await query.Select(pf => new CartableFinancialRow { PreFactor = pf, ReceiveAmount = db.Accountancies.AsNoTracking().ForPreFactor().NotVosoled().Where(a => a.pfid == pf.Id).Sum(f => f.AccType == AcEnum.واریزی ? f.credit : -f.BackWard) ?? 0, NotAcceptedFiAccCount = db.Accountancies.AsNoTracking().ForPreFactor().OnlyFinancial().Count(a => a.pfid == pf.Id && a.Status != AccStatusEnum.تایید_شده), NotAcceptedMdAccCount = db.Accountancies.AsNoTracking().ForPreFactor().OnlyTamin().Count(a => a.pfid == pf.Id && a.Status != AccStatusEnum.تایید_شده), TotalAccCount = db.Accountancies.AsNoTracking().ForPreFactor().Count(a => a.pfid == pf.Id), }).ToListAsync(ct);
        return new(result, filterCounts);
    }

    public async Task<CartableSalesData> GetSalesAsync(string? search, int type, int searchNumber, CancellationToken ct)
    {
        var baseQuery = db.PreFactors.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var serialVal = searchNumber;
            baseQuery = baseQuery.Where(s => EF.Functions.Like(s.AgentName, $"%{search}%") || EF.Functions.Like(s.Customer, $"%{search}%") || s.SerialNumber == serialVal);
        }

        const int pending = (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش;
        var approved = new[]
        {
            (int)PreFactorEnum.در_انتظار_تولید,
            (int)PreFactorEnum.جنس_به_مشتری_تحویل_داده_شد,
            (int)PreFactorEnum.در_انتظار_تایید_مالی
        };
        var filterCounts = new Dictionary<int, int>
        {
            [-1] = await baseQuery.Where(s => approved.Contains(s.Pftype) || s.Pftype == pending).CountAsync(ct),
            [1] = await baseQuery.Where(s => s.Pftype == pending).CountAsync(ct),
            [2] = await baseQuery.Where(s => approved.Contains(s.Pftype)).CountAsync(ct),
        };
        var query = type switch
        {
            -1 => baseQuery.Where(s => approved.Contains(s.Pftype) || s.Pftype == pending),
            1 => baseQuery.Where(s => s.Pftype == pending),
            2 => baseQuery.Where(s => approved.Contains(s.Pftype)),
            _ => baseQuery
        };
        var pList = await (
            from pf in query
            let receiveAmount = db.Accountancies.ForPreFactor().NotVosoled().Where(a => a.pfid == pf.Id).Sum(f => f.AccType == AcEnum.واریزی ? (f.credit ?? 0) : -(f.BackWard ?? 0))select new CartableSalesRow
            {
                PreFactor = pf,
                ReceiveAmount = receiveAmount,
                SumTotalAmountTax = pf.SumTotalAmount + pf.TotalTax - pf.Discount,
                PaymentAmount = pf.SumTotalAmount + pf.TotalTax - pf.Discount - receiveAmount
            }

        ).OrderByDescending(f => f.PreFactor.AcceptedFinancial == false).ThenByDescending(f => f.ReceiveAmount != 0).ThenByDescending(f => f.PreFactor.ModifyDate).Take(100).ToListAsync(ct);
        return new(pList, filterCounts);
    }
}
