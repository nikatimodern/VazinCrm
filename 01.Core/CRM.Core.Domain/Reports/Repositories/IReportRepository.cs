#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.Reports.Models;

namespace CRM.Core.Domain.Reports.Repositories;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Report?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<List<Report>> SearchAsync(ReportSearch search, Guid? dealId, string userId, bool canReadAllSatisfaction, CancellationToken cancellationToken);
    Task<List<Report>> GetByPhoneAsync(string phone, CancellationToken cancellationToken);
    Task PopulateUserImagesAsync(List<Report> reports, CancellationToken cancellationToken);
    Task<ReportBaseItems> GetBaseItemsAsync(CancellationToken cancellationToken);
    Task<string?> GetCallRecordingPathAsync(Guid callId, CancellationToken cancellationToken);
    void Add(Report report);
    void Update(Report original, Report updated);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
