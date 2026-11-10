using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.Infrastructures.Sql;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services;

public class PreFactorService(
    CRMContextDb db,
    Sepidar1Service sepidar1Service,
    DateTimeServices dtService,
    IHubContext<TaskHub> hub,
    LogSystemService logSystemService) {
    public IQueryable<PreFactor> BuildQuery()
    {
        return db.PreFactors
            .Include(x => x.Deal)
            .Include(x => x.CustomerEntity)
            .OrderByDescending(x => x.DateMiladi)
            .ThenByDescending(x => x.CreateDate);
    }

    public async Task FillDetailsAsync(IList<PreFactor> data)
    {
        if (data == null || data.Count == 0)
            return;

        var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();

        var preFactorIds = data.Select(x => x.Id)
            .ToArray();

        // Accountancy
        var accountancyData = await db.Accountancies
            .AsNoTracking()
            .ForPreFactor()
            .NotVosoled()
            .Where(x => x.pfid.HasValue && preFactorIds.Contains(x.pfid.Value))
            .GroupBy(x => x.pfid)
            .Select(g => new {
                PreFactorId = g.Key,
                ReciveAmount = g.Sum(x => x.AccType == AcEnum.واریزی ? x.credit : -x.BackWard)
            })
            .ToDictionaryAsync(x => x.PreFactorId!.Value, x => x.ReciveAmount ?? 0);

        // Factor Counts
        Dictionary<Guid, int> factorCounts = new();

        if (setting?.CrmFactors == true)
        {
            factorCounts = await db.Factors
                .AsNoTracking()
                .Where(x => preFactorIds.Contains(x.PfactorID))
                .GroupBy(x => x.PfactorID)
                .Select(g => new {
                    PreFactorId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.PreFactorId, x => x.Count);
        }

        // Sepidar
        Dictionary<Guid, int> sepidarCounts = null;
        if (setting?.Sepidar == true)
        {
            var serialNumbers = data
                .Select(x => x.SerialNumber)
                .ToArray();

            var invoices = await sepidar1Service.GetInvoicesByQuotationNumbersAsync(serialNumbers);

            if (invoices is not null)
            {
                sepidarCounts = data.ToDictionary(
                    pf => pf.Id,
                    pf => invoices.Count(x => x.QuotationNumber == pf.SerialNumber && x.CreationDate >= pf.CreateDate)
                );
            }
        }

        foreach (var item in data)
        {
            accountancyData.TryGetValue(item.Id, out var receiveAmount);

            item.ReciveAmount = receiveAmount;

            item.PaymentAmount = item.SumTotalAmount + item.TotalTax - item.Discount - item.ReciveAmount;

            item.SumTotalAmountTax = item.SumTotalAmount + item.TotalTax - item.Discount;

            if (setting?.Sepidar == true && sepidarCounts is not null)
            {
                sepidarCounts.TryGetValue(item.Id, out var count);

                item.CountFactorSepidar = count;
            }

            if (setting?.CrmFactors == true)
            {
                factorCounts.TryGetValue(item.Id, out var count);

                item.CountFactor = count;
            }
        }
    }


    /// <param name="pf">پیش فاکتور</param>
    /// <param name="type">1: accept 2: reject</param>
    public async Task<PreFactor> AcceptOrRejectPreFactor(PreFactor pf, int type)
    {
        var rejectDict = new Dictionary<int, int> {
            { (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین, (int)PreFactorEnum.در_انتظار_واگذاری_به_مسئول_تامین }, // عدم تایید تامین
            { (int)PreFactorEnum.در_انتظار_ارسال_به_مشتری, (int)PreFactorEnum.در_انتظار_واگذاری_به_مسئول_تامین }, // عدم تایید تامین
            { (int)PreFactorEnum.در_انتظار_تایید_مشتری, (int)PreFactorEnum.در_انتظار_واگذاری_به_مسئول_تامین }, // عدم تایید تامین
            { (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش, (int)PreFactorEnum.در_انتظار_ارسال_به_مشتری }, // عدم تایید مدیرفروش
            { (int)PreFactorEnum.در_انتظار_تایید_مالی, (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش }, // عدم تایید مالی
            { (int)PreFactorEnum.در_انتظار_تولید, (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش }, // عدم تایید مالی
        };

        var acceptedDict = new Dictionary<int, int> {
            { (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین, (int)PreFactorEnum.در_انتظار_ارسال_به_مشتری }, // تایید تامین
            { (int)PreFactorEnum.در_انتظار_تایید_مشتری, (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش }, // تایید مشتری
            { (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش, (int)PreFactorEnum.در_انتظار_تایید_مالی }, // تایید مدیرفروش
            { (int)PreFactorEnum.در_انتظار_تایید_مالی, (int)PreFactorEnum.در_انتظار_تولید }, // تایید مالی
        };


        var targetDict = type switch {
            1 => acceptedDict,
            2 => rejectDict,
            _ => null
        };

        if (targetDict == null) throw new InvalidOperationException($"Invalid type value: {type}. Expected 1 (accept) or 2 (reject).");

        var currentStatus = pf.Pftype;
        if (targetDict.TryGetValue(pf.Pftype, out var newStatus))
        {
            if (type == 2)
                pf.RecordReturn();

            pf.PrePftype = currentStatus;
            pf.Pftype = newStatus;
        } else
        {
            throw new KeyNotFoundException($"Current status '({currentStatus}) is not defined in '{(type == 1 ? "acceptedDict" : "rejectDict")}'");
        }

        pf.PftypeTxt = Service.GetTypeTxtPfactor(pf.Pftype);

        await db.SaveChangesAsync();

        await hub.Clients.All.SendAsync("countOfPendingCartable");
        return pf;
    }


    public async Task DeletePreFactor(PreFactor pf, AppUser user)
    {
        try
        {
            pf.IsDeleted = true;
            pf.PrePftype = pf.Pftype;
            pf.Pftype = (int)PreFactorEnum.حذف_شده;
            pf.PftypeTxt = nameof(PreFactorEnum.حذف_شده).Replace("_", " ");

            pf.UserModify = user.FullName;
            pf.UserModifyID = user.Id;
            pf.ModifyDate = DateTime.Now;
            pf.ModifyDateTxt = dtService.GetDateTimePersian();

            pf.CommentText = "حذف شده توسط" + " " + user.FullName;

            var orderHeads = await db.OrderHeads.Where(f => f.Prefactor == pf.Id).ToListAsync();
            foreach (var i in orderHeads)
            {
                i.IsDeleted = true;
                if (string.IsNullOrEmpty(user.Id))
                {
                    i.CommentText = "حذف شده توسط سیستم";
                }
            }

            var accS = await db.Accountancies.ForPreFactor(pf.Id).ToListAsync();

            foreach (var acc in accS.Where(f => f.CustomerId != null && f.PayType != PayTypeEnum.اعتباری))
                acc?.Wallet = true;

            foreach (var acc in accS.Where(f => f.PayType == PayTypeEnum.اعتباری))
                acc?.IsDeleted = true;


            await logSystemService.LogChange(pf, null, LogActionEnum.حذف, user);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static PreFactor UpdateTaxAmountPreFactor(PreFactor pf)
    {
        pf.PercentTax = pf.OfficialType == 1 ? 10 : 0;
        pf.TotalTax = pf.OfficialType == 1 ? pf.SumTotalAmount * 10 / 100 : 0;
        pf.PaymentAmount = pf.SumTotalAmount + pf.TotalTax;
        return pf;
    }
}
