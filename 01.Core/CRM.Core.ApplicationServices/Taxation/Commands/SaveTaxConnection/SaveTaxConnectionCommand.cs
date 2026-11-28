using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxConnection;

public sealed class SaveTaxConnectionCommand : ICommand<bool>
{
    public Guid MemoryId { get; init; }
    public TaxAuthorityProtocol Protocol { get; init; }
    public string SellerEconomicCode { get; init; } = "";
    public string? CertificateSha256 { get; init; }
    public string? V1PrivateKey { get; init; }
}

public interface ITaxConnectionSettings
{
    TaxAuthorityProtocol ActiveProtocol { get; }
    Task SaveAsync(SaveTaxConnectionCommand request, Func<TaxAuthorityProtocol, CancellationToken, Task<bool>> hasOpenSubmissions, CancellationToken ct);
}

public sealed class SaveTaxConnectionCommandHandler(ITaxConnectionSettings settings, ITaxationRepository taxation,
    ITaxSubmissionRepository submissions) : ICommandHandler<SaveTaxConnectionCommand, bool>
{
    public async Task<bool> HandleAsync(SaveTaxConnectionCommand request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Protocol) || request.SellerEconomicCode is not { Length: 11 or 14 } ||
            !request.SellerEconomicCode.All(char.IsAsciiDigit))
            throw new ArgumentException("روش ارسال و شناسه اقتصادی معتبر وارد کنید.");

        var memories = await taxation.ListMemoriesAsync(ct);
        if (!memories.Any(x => x.Id == request.MemoryId))
            throw new KeyNotFoundException();
        await settings.SaveAsync(request, submissions.HasOpenSubmissionsAsync, ct);
        return true;
    }
}
