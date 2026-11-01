using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.Cartables.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Accountancies.Abstractions;
using CRM.Core.ApplicationServices.Accountancies.Dtos;
using CRM.Core.ApplicationServices.Accountancies.Services;

namespace CRM.Core.ApplicationServices.Accountancies.Queries.GetInvoiceTotals;
public sealed record GetInvoiceTotalsQuery(int Type, Guid? Id) : IQuery<PaymentTotalsData?>;
public sealed class GetInvoiceTotalsQueryHandler(IAccountancyRepository repository) : IQueryHandler<GetInvoiceTotalsQuery, PaymentTotalsData?>
{
    public async Task<PaymentTotalsData?> HandleAsync(GetInvoiceTotalsQuery request, CancellationToken ct)
    {
        var data = await repository.GetTotalsAsync(request.Type, request.Id, ct);
        if (data == null)
            return null;
        var total = data.TotalAmount + data.Tax - data.Discount;
        var payable = total - data.Received + data.Returned;
        if (request.Type != 2)
            payable = Math.Max(0, payable);
        return new()
        {
            Discount = data.Discount,
            PaymentAmount = payable,
            ReciveAmount = data.Received,
            BackwardAmount = data.Returned,
            SumTotalAmount = data.TotalAmount,
            SumTotalAmountTax = total,
            TotalTax = data.Tax
        };
    }
}
