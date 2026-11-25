using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Infrastructures.Sql.Taxation;

public sealed class TaxReportRepository(CRMContextDb db) : ITaxReportRepository
{
    public async Task<TaxReportData> ReadAsync(TaxReportFilter filter, int pageSize, bool export, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var factors = db.Factors.AsNoTracking().AsQueryable();
        if (filter.From is { } from)
            factors = factors.Where(x => x.DateMiladi >= from.Date);
        if (filter.Through is { } through)
        {
            var cutoff = through.Date.AddDays(1);
            factors = factors.Where(x => x.DateMiladi < cutoff);
        }
        if (filter.Customer.Length > 0)
            factors = factors.Where(x => x.Customer.Contains(filter.Customer));

        var environment = filter.Scope switch
        {
            TaxReportScope.Sandbox => TaxAuthorityEnvironment.Sandbox,
            TaxReportScope.Production => TaxAuthorityEnvironment.Production,
            _ => (TaxAuthorityEnvironment?)null
        };
        var dispatches = db.TaxDispatches.AsNoTracking();
        var scoped = dispatches.Where(x => x.AuthorityEnvironment == environment);
        if (filter.MemoryId.HasValue)
            scoped = scoped.Where(x => x.MemoryId == filter.MemoryId.Value);
        if (filter.Kind.HasValue)
            scoped = scoped.Where(x => x.Kind == filter.Kind.Value);
        var rows = from document in scoped
                   join factor in factors on document.FactorId equals factor.Id
                   select new ReportEntry
                   {
                       FactorId = factor.Id, DispatchId = document.Id, Number = factor.SerialNumber, Customer = factor.Customer,
                       Date = factor.DateMiladi, Amount = factor.PaymentAmount, Currency = factor.CurrencyType,
                       Kind = document.Kind, FiscalId = document.FiscalId, TaxId = document.TaxId,
                       Acknowledged = document.ProtectedAcknowledgement != null,
                       Status = document.Status == TaxDispatchStatus.Pending
                           ? (document.TaxId == null ? TaxReportStatus.Queued : TaxReportStatus.Ready)
                           : document.Status == TaxDispatchStatus.InFlight
                               ? (document.LeaseUntilUtc <= now ? TaxReportStatus.Unknown : TaxReportStatus.Sending)
                               : document.Status == TaxDispatchStatus.Submitted ? TaxReportStatus.Waiting
                               : document.Status == TaxDispatchStatus.Accepted ? TaxReportStatus.Accepted
                               : document.Status == TaxDispatchStatus.Rejected ? TaxReportStatus.Rejected : TaxReportStatus.Unknown
                   };
        if (filter.Scope == TaxReportScope.Local && filter.Kind is null or TaxDispatchKind.Original)
        {
            var active = await db.TaxMemories.AsNoTracking().Where(x => x.IsActive).Select(x => new { x.Id, x.FiscalId })
                .SingleOrDefaultAsync(ct);
            if (!filter.MemoryId.HasValue || filter.MemoryId == active?.Id)
            {
                var fiscalId = active?.FiscalId;
                var candidates = factors.Where(x => !x.IsDeleted && x.OfficialType == 1 && x.status == FactorStatus.تایید_شده &&
                    x.ParentReturnedId == null && !dispatches.Any(dispatch => dispatch.FactorId == x.Id));
                rows = rows.Concat(candidates.Select(factor => new ReportEntry
                {
                    FactorId = factor.Id, DispatchId = null, Number = factor.SerialNumber, Customer = factor.Customer,
                    Date = factor.DateMiladi, Amount = factor.PaymentAmount, Currency = factor.CurrencyType,
                    Kind = TaxDispatchKind.Original, FiscalId = fiscalId, TaxId = null, Acknowledged = false,
                    Status = fiscalId == null || factor.Export == true || factor.CustomerId == null ||
                        factor.CurrencyType != CurrencyTypeEnum.ریال || factor.DateMiladi == null ||
                        factor.Cus_EghtesadiCode == null || factor.Cus_EghtesadiCode == "" ||
                        !factor.FactorItems.Any(line => !line.IsDeleted) ||
                        factor.FactorItems.Any(line => !line.IsDeleted &&
                            !db.TaxProductMappings.Any(mapping => mapping.IsDefault && mapping.ProductId == line.ProductCode &&
                                mapping.Unit == line.Unit && mapping.TaxUnit.Length <= 8))
                        ? TaxReportStatus.NeedsData : TaxReportStatus.NeedsReview
                }));
            }
        }

        if (filter.Status.HasValue)
            rows = rows.Where(x => x.Status == filter.Status.Value);

        var stats = await rows.GroupBy(x => x.Status).Select(group => new TaxReportCount(group.Key, group.Count())).ToListAsync(ct);
        var total = stats.Sum(x => x.Count);
        if (export && total > pageSize)
            throw new ArgumentException("برای دانلود بیش از ۵۰۰۰ ردیف، بازه گزارش را محدود کنید.");
        var pageCount = Math.Max(1, (int)Math.Ceiling(total / (decimal)pageSize));
        var page = export ? 1 : Math.Min(filter.Page, pageCount);
        var visible = await rows.OrderByDescending(x => x.Date).ThenByDescending(x => x.Number)
            .ThenBy(x => x.FactorId).ThenBy(x => x.DispatchId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var acknowledged = await rows.CountAsync(x => x.Acknowledged, ct);
        var references = await rows.CountAsync(x => x.Kind != TaxDispatchKind.Original, ct);
        var payments = await db.TaxPaymentDispatches.AsNoTracking()
            .CountAsync(payment => rows.Any(row => row.DispatchId == payment.InvoiceDispatchId), ct);
        return new(visible.Select(x => new TaxReportRow(x.FactorId, x.DispatchId, x.Number, x.Customer, x.Date, x.Amount,
            x.Currency.ToString().Replace('_', ' '), x.Kind, x.FiscalId, x.TaxId, x.Status, x.Acknowledged)).ToArray(),
            stats, acknowledged, references, payments, page, pageCount, total);
    }

    private sealed class ReportEntry
    {
        public Guid FactorId { get; init; }
        public Guid? DispatchId { get; init; }
        public int Number { get; init; }
        public string Customer { get; init; }
        public DateTime? Date { get; init; }
        public decimal Amount { get; init; }
        public CurrencyTypeEnum Currency { get; init; }
        public TaxDispatchKind Kind { get; init; }
        public string FiscalId { get; init; }
        public string TaxId { get; init; }
        public TaxReportStatus Status { get; init; }
        public bool Acknowledged { get; init; }
    }
}
