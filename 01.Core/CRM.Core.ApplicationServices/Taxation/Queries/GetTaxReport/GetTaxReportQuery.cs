using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Queries.GetTaxReport;

public sealed record GetTaxReportQuery(TaxReportFilter Filter, bool Export = false) : IQuery<TaxReportData>;

public sealed class GetTaxReportQueryHandler(ITaxReportRepository repository) : IQueryHandler<GetTaxReportQuery, TaxReportData>
{
    public Task<TaxReportData> HandleAsync(GetTaxReportQuery request, CancellationToken ct)
    {
        var filters = request.Filter;
        var customer = filters.Customer?.Trim() ?? string.Empty;
        if (customer.Length > 100 || filters.From?.Date > filters.Through?.Date ||
            filters.Through?.Date == DateTime.MaxValue.Date ||
            !Enum.IsDefined(filters.Scope) || (filters.Kind.HasValue && !Enum.IsDefined(filters.Kind.Value)) ||
            (filters.Status.HasValue && !Enum.IsDefined(filters.Status.Value)))
            throw new ArgumentException("فیلترهای گزارش معتبر نیستند.");
        return repository.ReadAsync(filters with { Customer = customer, Page = Math.Max(1, filters.Page) },
            request.Export ? 5000 : 25, request.Export, ct);
    }
}
