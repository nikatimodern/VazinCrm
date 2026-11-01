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

namespace CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestAmountDetails;
public sealed record GetRequestAmountDetailsQuery(Guid Id) : IQuery<RequestAmountRow?>;
public sealed class GetRequestAmountDetailsQueryHandler(IRequestAmountRepository repository) : IQueryHandler<GetRequestAmountDetailsQuery, RequestAmountRow?>
{
    public async Task<RequestAmountRow?> HandleAsync(GetRequestAmountDetailsQuery request, CancellationToken ct)
    {
        return await repository.GetDetailsAsync(request.Id, ct);
    }
}
