using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.SetTaxProductMapping;

public sealed record SetTaxProductMappingCommand(Guid ProductId, int Unit, string GoodsId, string TaxUnit) : ICommand<bool>;

public sealed class SetTaxProductMappingCommandHandler(ITaxationRepository repository) : ICommandHandler<SetTaxProductMappingCommand, bool>
{
    public async Task<bool> HandleAsync(SetTaxProductMappingCommand request, CancellationToken ct)
    {
        if (request.GoodsId is not { Length: 13 } || !request.GoodsId.All(char.IsAsciiDigit) ||
            request.Unit < 0 || request.TaxUnit is not { Length: > 0 and <= 8 } || !request.TaxUnit.All(char.IsAsciiDigit))
            throw new ArgumentException("کد کالا یا واحد مالیاتی نامعتبر است.");
        await repository.SetMappingAsync(request.ProductId, request.Unit, request.GoodsId, request.TaxUnit, ct);
        return true;
    }
}
