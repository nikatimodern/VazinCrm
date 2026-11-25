using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxMemorySettings;

public sealed class TaxMemorySettingsInput
{
    public Guid? Id { get; init; }
    public string Title { get; init; }
    public string FiscalId { get; init; }
    public TaxAuthorityEnvironment Environment { get; init; }
    public TaxAuthorityProtocol Protocol { get; init; }
    public string SellerEconomicCode { get; init; }
    public bool SandboxEnabled { get; init; }
    public DateTime? ExpectedUpdatedUtc { get; init; }
    public string? CertificateSha256 { get; init; }
    public string? PrivateKeyPem { get; init; }
    public string? PublicKeyPem { get; init; }
    public byte[]? Pfx { get; init; }
    public string? PfxPassword { get; init; }
}

public interface ITaxMemoryCredentials
{
    string Prepare(TaxMemorySettingsInput input, TaxMemory? current);
    IReadOnlyList<TaxCertificateOption> ListCertificates();
    TaxMemoryCredentialStatus Describe(TaxMemory memory);
}

public sealed record TaxCertificateOption(string Fingerprint, string Subject, string Issuer, DateTime ExpiresUtc, bool PrivateKeyAccessible);
public sealed record TaxMemoryCredentialStatus(bool Ready, string Message);

public sealed record SaveTaxMemorySettingsCommand(TaxMemorySettingsInput Input, string ActorId) : ICommand<Guid>;

public sealed class SaveTaxMemorySettingsCommandHandler(ITaxMemorySettingsRepository repository, ITaxMemoryCredentials credentials)
    : ICommandHandler<SaveTaxMemorySettingsCommand, Guid>
{
    public async Task<Guid> HandleAsync(SaveTaxMemorySettingsCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ActorId) || request.ActorId.Length > 450)
            throw new ArgumentException("شناسه مدیر قابل ثبت نیست.");
        var input = request.Input;
        var title = input.Title?.Trim();
        var fiscalId = input.FiscalId?.Trim().ToUpperInvariant();
        var seller = input.SellerEconomicCode?.Trim();
        if (title is not { Length: > 0 and <= 120 } || fiscalId is not { Length: 6 } ||
            !fiscalId.All(char.IsAsciiLetterOrDigit) || seller is not { Length: 11 or 14 } || !seller.All(char.IsAsciiDigit) ||
            !Enum.IsDefined(input.Environment) || !Enum.IsDefined(input.Protocol))
            throw new ArgumentException("عنوان، شناسه حافظه، شناسه اقتصادی، محیط و روش ارسال معتبر وارد کنید.");
        if (input.SandboxEnabled && input.Environment != TaxAuthorityEnvironment.Sandbox)
            throw new ArgumentException("ارتباط با محیط عملیاتی غیرفعال است.");

        var existing = input.Id is { } id ? await repository.FindMemoryAsync(id, ct) ?? throw new KeyNotFoundException() : null;
        if (existing is not null && existing.UpdatedUtc != input.ExpectedUpdatedUtc)
            throw new TaxPreviewConflictException("تنظیمات حافظه تغییر کرده است. صفحه را دوباره بارگذاری کنید.");

        var fingerprint = credentials.Prepare(input, existing);
        return await repository.SaveSettingsAsync(new(input.Id, title, fiscalId, input.Environment, input.Protocol,
            seller, fingerprint, input.SandboxEnabled, input.ExpectedUpdatedUtc), request.ActorId, ct);
    }
}
