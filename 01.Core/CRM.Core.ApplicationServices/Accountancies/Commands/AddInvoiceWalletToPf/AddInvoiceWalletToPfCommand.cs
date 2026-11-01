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

namespace CRM.Core.ApplicationServices.Accountancies.Commands.AddInvoiceWalletToPf;
public sealed record AddInvoiceWalletToPfCommand(Guid Id, Guid PfId) : ICommand<AccountancyOutcome>;
public sealed class AddInvoiceWalletToPfCommandHandler(IAccountancyRepository repository) : ICommandHandler<AddInvoiceWalletToPfCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(AddInvoiceWalletToPfCommand request, CancellationToken ct)
    {
        var id = request.Id;
        var pfId = request.PfId;
        try
        {
            var acc = await repository.GetAsync(id, false, true, ct);
            var pf = await repository.GetPreFactorAsync(pfId, ct);
            if (acc == null || pf == null)
                return new(404, "رکوردی یافت نشد!");
            acc.Wallet = false;
            acc.pfid = pfId;
            acc.pfSerialNumber = pf.SerialNumber;
            acc.DealId = pf.DealId;
            acc.DealSerialNumber = pf.DealSerialNumber;
            await repository.SaveAsync(ct);
            return new(200);
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new(400);
        }
    }
}
