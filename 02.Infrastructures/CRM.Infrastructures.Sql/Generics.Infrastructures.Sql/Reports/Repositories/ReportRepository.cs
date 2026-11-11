#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using Generics.Infrastructures.Sql;
using CRM.Core.Domain.Reports.Models;
using CRM.Core.Domain.Reports.Enum;
using CRM.Core.Domain.BaseItems.Enums;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.Reports.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Reports.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly CRMContextDb _db;
    private readonly UserContextDb _users;

    public ReportRepository(CRMContextDb db, UserContextDb users)
    {
        _db = db;
        _users = users;
    }

    public Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Reports.AsNoTracking()
            .FirstOrDefaultAsync(report => report.Id == id, cancellationToken);
    }

    public Task<Report?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Reports.FirstOrDefaultAsync(report => report.Id == id, cancellationToken);
    }


    public async Task<List<Report>> SearchAsync(ReportSearch search, Guid? dealId, string userId, bool canReadAllSatisfaction, CancellationToken cancellationToken)
    {
        var qData = _db.Reports.AsNoTracking().Include(r => r.CallRecording.Participants)
            .OrderByDescending(r => r.RegDate).AsQueryable();
        if (dealId != null)
        {
            qData = qData.Where(f => f.DealId == dealId);
        } else
        {

            if (!string.IsNullOrWhiteSpace(search.Search))
            {
                var val = $"%{search.Search}%";
                qData = qData.Where(r =>
                    EF.Functions.Like(r.CustomerTxt, val) ||
                    EF.Functions.Like(r.ReportText, val) ||
                    EF.Functions.Like(r.RepTel, val)
                );
            }

            if (search.Type != -1)
            {
                qData = qData.Where(r => r.ReportType == (ReportTypeEnum)search.Type);
            }

            if (search.Type == 1)
            {
                if (search.ResultCallId != null)
                {
                    qData = qData.Where(f => f.ResultCallId == search.ResultCallId);
                }

                if (search.FinalResultCallId != null)
                {
                    qData = qData.Where(f => f.FinalResultCallId == search.FinalResultCallId);
                }

                if (search.ReasonCancelId != null)
                {
                    qData = qData.Where(f => f.ReasonCancelId == search.ReasonCancelId);
                }
            }


            if (search.FilterCustomer) qData = qData.Where(r => r.CustomerId == search.CustomerId);
            if (search.FilterUser) qData = qData.Where(r => r.UserCreatorID == search.UserId);
            if (search.FilterDate) qData = qData.Where(r => r.CreateDate >= search.FromDate && r.CreateDate <= search.ToDate);

            if (!canReadAllSatisfaction)
            {
                qData = qData.Where(r =>
                    (r.ReportType == ReportTypeEnum.گزارش_رضایتمندی && r.UserCreatorID == userId) ||
                    r.ReportType != ReportTypeEnum.گزارش_رضایتمندی
                );
            }

            if (search.Count != -1)
                qData = qData.Take(search.Count);
        }

        return await qData.ToListAsync(cancellationToken);
    }

    public Task<List<Report>> GetByPhoneAsync(string phone, CancellationToken cancellationToken) =>
        _db.Reports.AsNoTracking().Where(r => r.RepTel == phone).Include(r => r.CallRecording.Participants)
            .OrderByDescending(r => r.CreateDate).ToListAsync(cancellationToken);

    public async Task PopulateUserImagesAsync(List<Report> qData, CancellationToken cancellationToken)
    {
        var userIds = qData.Select(r => r.UserCreatorID)
            .Union(qData.Where(r => r.CallRecording != null).SelectMany(r => r.CallRecording.Participants.Select(p => p.UserId)))
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        var userImages = await _users.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.PersonImageUrl, cancellationToken);

        foreach (var r in qData)
        {
            if (!string.IsNullOrEmpty(r.UserCreatorID) && userImages.TryGetValue(r.UserCreatorID, out var img))
            {
                r.UserImage = img;
            }

            foreach (var p in r.CallRecording?.Participants ?? [])
            {
                if (!string.IsNullOrEmpty(p.UserId) && userImages.TryGetValue(p.UserId, out var pImg))
                {
                    p.UserImage = pImg;
                }
            }
        }

    }

    public async Task<ReportBaseItems> GetBaseItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _db.BaseItems.AsNoTracking().Where(x => x.TypeCode == BaseItemEnum.نتیجه_تماس ||
            x.TypeCode == BaseItemEnum.نتیجه_نهایی || x.TypeCode == BaseItemEnum.علت_انصراف).ToListAsync(cancellationToken);
        List<ReportLookup> Select(BaseItemEnum type) => items.Where(x => x.TypeCode == type).Select(x => new ReportLookup(x.Id, x.Title)).ToList();
        return new(Select(BaseItemEnum.نتیجه_تماس), Select(BaseItemEnum.نتیجه_نهایی), Select(BaseItemEnum.علت_انصراف));
    }

    public Task<string?> GetCallRecordingPathAsync(Guid callId, CancellationToken cancellationToken) =>
        _db.CallRecordings.AsNoTracking().Where(x => x.Id == callId).Select(x => x.RecordingFilePath).FirstOrDefaultAsync(cancellationToken);

    public void Add(Report report)
    {
        _db.Reports.Add(report);
    }

    public void Update(Report original, Report updated)
    {
        _db.Entry(original).CurrentValues.SetValues(updated);
        _db.Entry(original).Property(report => report.CreateDate).IsModified = false;
        _db.Entry(original).Property(report => report.CreateDateTxt).IsModified = false;
        _db.Entry(original).Property(report => report.UserCreator).IsModified = false;
        _db.Entry(original).Property(report => report.UserCreatorID).IsModified = false;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}
