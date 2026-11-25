using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxMemory;

public sealed record SaveTaxMemoryCommand(Guid? Id, string FiscalId, string Title) : ICommand<Guid>;

public sealed class SaveTaxMemoryCommandHandler(ITaxationRepository repository) : ICommandHandler<SaveTaxMemoryCommand, Guid>
{
    public async Task<Guid> HandleAsync(SaveTaxMemoryCommand request, CancellationToken ct)
    {
        var code = request.FiscalId?.Trim().ToUpperInvariant();
        var title = request.Title?.Trim();
        if (code is not { Length: 6 } || !code.All(char.IsAsciiLetterOrDigit))
            throw new ArgumentException("شناسه حافظه باید شش حرف یا رقم انگلیسی باشد.");
        if (string.IsNullOrEmpty(title) || title.Length > 120)
            throw new ArgumentException("عنوان حافظه باید بین یک تا ۱۲۰ نویسه باشد.");
        return await repository.SaveMemoryAsync(request.Id, code, title, ct);
    }
}
