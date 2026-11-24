using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.Taxation;

public enum TaxReportScope { Local, Sandbox, Production }
public enum TaxReportStatus { NeedsReview, NeedsData, Queued, Ready, Sending, Waiting, Accepted, Rejected, Unknown }

public sealed record TaxReportFilter(DateTime? From, DateTime? Through, string Customer, TaxDispatchKind? Kind,
    Guid? MemoryId, TaxReportStatus? Status, TaxReportScope Scope, int Page = 1);

public sealed record TaxReportRow(Guid FactorId, Guid? DispatchId, int Number, string Customer, DateTime? Date,
    decimal Amount, string Currency, TaxDispatchKind Kind, string FiscalId, string TaxId, TaxReportStatus Status,
    bool Acknowledged);

public sealed record TaxReportCount(TaxReportStatus Status, int Count);

public sealed record TaxReportData(IReadOnlyList<TaxReportRow> Rows, IReadOnlyList<TaxReportCount> Statuses,
    int Acknowledged, int References, int Payments, int Page, int PageCount, int Total);

public interface ITaxReportRepository
{
    Task<TaxReportData> ReadAsync(TaxReportFilter filter, int pageSize, bool export, CancellationToken ct);
}
