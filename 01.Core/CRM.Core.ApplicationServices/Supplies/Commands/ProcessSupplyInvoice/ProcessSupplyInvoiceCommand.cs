using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Commands.ProcessSupplyInvoice;
public sealed record ProcessSupplyInvoiceCommand(Attachment Attachment) : ICommand<SupplyResult>;
public sealed class ProcessSupplyInvoiceCommandHandler(ISupplyIntegrationServices integrations) : ICommandHandler<ProcessSupplyInvoiceCommand, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(ProcessSupplyInvoiceCommand command, CancellationToken cancellationToken) => integrations.ProcessInvoiceAsync(command.Attachment, cancellationToken);
}
