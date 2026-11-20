using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Commands.SetQualityControlCustomerContact;

public sealed record SetQualityControlCustomerContactCommand(SetQualityControlCustomerContactRequest Request)
    : ICommand<SetQualityControlCustomerContactResult>;

public sealed class SetQualityControlCustomerContactCommandHandler(
    IQualityControlRepository repository, CurrentActor actor, IApplicationDates dates)
    : ICommandHandler<SetQualityControlCustomerContactCommand, SetQualityControlCustomerContactResult>
{
    public async Task<SetQualityControlCustomerContactResult> HandleAsync(
        SetQualityControlCustomerContactCommand command, CancellationToken ct)
    {
        var request = command.Request;
        if (request.CustomerId == Guid.Empty ||
            request.Status is not (QualityControlCustomerContactStatus.NoResponse or QualityControlCustomerContactStatus.InvalidNumber))
            return new(400, "مشتری یا وضعیت تماس نامعتبر است.", QualityControlCustomerContactStatus.None);

        return await repository.SetCustomerContactStatusAsync(request,
            QualityControlAccessPolicy.For(await actor.GetAsync(ct)), dates.PersianNow(), ct);
    }
}
