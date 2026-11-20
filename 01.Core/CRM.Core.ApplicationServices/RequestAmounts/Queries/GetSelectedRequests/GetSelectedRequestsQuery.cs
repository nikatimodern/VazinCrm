using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.RequestAmounts.Enums;
using CRM.Core.Domain.RequestAmounts.Repositories;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Payments.Enums;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.ApplicationServices.RequestAmounts.Abstractions;
using CRM.Core.ApplicationServices.RequestAmounts.Services;
using CRM.Core.ApplicationServices.RequestAmounts.Dtos;
using CRM.Core.Domain.Querying;

namespace CRM.Core.ApplicationServices.RequestAmounts.Queries.GetSelectedRequests;
public sealed record SelectedRequests(List<RequestAmount> Data, string? Error);
public sealed record GetSelectedRequestsQuery(List<Guid> Ids) : IQuery<SelectedRequests>;
public sealed class GetSelectedRequestsQueryHandler(IRequestAmountRepository repository) : IQueryHandler<GetSelectedRequestsQuery, SelectedRequests>
{
    public async Task<SelectedRequests> HandleAsync(GetSelectedRequestsQuery request, CancellationToken ct)
    {
        var data = await repository.GetByIdsAsync(request.Ids, false, ct);
        // A grouped payment must share one target and one target entity kind.
        var targetCount = data.Select(r => (r.TargetPersonId, Kind: r.RequestAmountType.GetTargetKind())).Distinct().Count();
        return new(data, targetCount > 1 ? "در پرداخت گروهی تمام درخواست‌ها باید برای یک شخص و از یک نوع مقصد باشند." : null);
    }
}
