#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.Reports.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Reports.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly CRMContextDb _db;

    public ReportRepository(CRMContextDb db)
    {
        _db = db;
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
