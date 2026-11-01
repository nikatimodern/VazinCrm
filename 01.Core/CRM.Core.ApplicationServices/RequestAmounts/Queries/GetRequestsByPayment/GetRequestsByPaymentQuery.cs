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

namespace CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestsByPayment;
public sealed record GetRequestsByPaymentQuery(Guid Id) : IQuery<List<RequestAmount>>;
public sealed class GetRequestsByPaymentQueryHandler(IRequestAmountRepository repository) : IQueryHandler<GetRequestsByPaymentQuery, List<RequestAmount>>
{
    public async Task<List<RequestAmount>> HandleAsync(GetRequestsByPaymentQuery request, CancellationToken ct)
    {
        return await repository.GetByPaymentAsync(request.Id, ct);
    }
}
