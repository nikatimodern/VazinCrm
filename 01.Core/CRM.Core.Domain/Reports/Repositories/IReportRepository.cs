#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Reports.Entities;

namespace CRM.Core.Domain.Reports.Repositories;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Report?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
    void Add(Report report);
    void Update(Report original, Report updated);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
