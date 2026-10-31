using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.Orders.Repositories;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Orders.Services;
using CRM.Core.ApplicationServices.Orders.Abstractions;

namespace CRM.Core.ApplicationServices.Orders.Queries.GetOrderDocument;
public sealed record GetOrderDocumentQuery(Guid Id, bool Image) : IQuery<byte[]>;
public sealed class GetOrderDocumentQueryHandler(IOrderEffects effects) : IQueryHandler<GetOrderDocumentQuery, byte[]>
{
    public async Task<byte[]> HandleAsync(GetOrderDocumentQuery request, CancellationToken ct)
    {
        return await effects.RenderDocumentAsync(request.Id, request.Image, ct);
    }
}
