using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accountancies.Abstractions;
using CRM.Core.ApplicationServices.Accountancies.Dtos;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.Accountancies.Enum;

namespace CRM.Core.ApplicationServices.Accountancies.Commands.RemoveVosolInvoice;

public sealed record RemoveVosolInvoiceCommand(Guid AccountancyId) : ICommand<AccountancyOutcome>;

public sealed class RemoveVosolInvoiceCommandHandler(IAccountancyRepository repository, IDepositFinancialRepository financial)
    : ICommandHandler<RemoveVosolInvoiceCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(RemoveVosolInvoiceCommand request, CancellationToken ct)
    {
        await using var guard = await financial.LockAsync(request.AccountancyId, ct);
        var source = await financial.GetDepositAsync(request.AccountancyId, ct);
        if (source == null)
            return new(404);
        DepositOriginalCheque? original = null;
        if (source.PayType == PayTypeEnum.چکی && source.Vosoled)
        {
            var mappings = await financial.GetMappingsAsync(source.Id, null, null, ct);
            if (mappings.Count > 0)
            {
                original = await financial.ReadOriginalChequeAsync(mappings, ct);
                if (original == null)
                    return new(409, "مبلغ اولیه یا سررسید چک در سوابق سپیدار قابل تطبیق نیست. ابتدا سوابق رسید را بررسی کنید.");
            }
        }
        return await repository.RemoveVosolAsync(request.AccountancyId, ct, original) ? new(200) : new(404);
    }
}
