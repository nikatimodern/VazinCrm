using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using static CRM.EndPoint.FrontEnd.Services.SepidarServices.SepidarService;

namespace CRM.EndPoint.FrontEnd.Services.Accountancies;

public sealed record DepositSynchronizationRequest(Guid Id, Guid SoftwareId, Guid MappingId,
    DepositChequeAction Action, string? Token);

public sealed partial class DepositFinancialService
{
    private static DepositSyncSource GetSyncSource(Accountancy source, Guid softwareId, IReadOnlyList<ExternalMapping> mappings)
    {
        var transfer = ActiveChequeTransfer(source);
        int GetTarget(Guid? id, ExternalMappingTypeEnum type, string entity)
        {
            if (!id.HasValue) return 0;
            var matches = mappings.Where(x => x.ExternalSoftwareId == softwareId && x.EntityId == id &&
                x.MapType == type && x.ExEntityName == entity).Take(2).ToArray();
            return matches.Length == 1 && int.TryParse(matches[0].ExEntityId, out var value) && value > 0 ? value : 0;
        }
        var cheque = source.PayType == PayTypeEnum.چکی;
        return new((int)source.PayType, source.credit!.Value, (SourceDate(source) ?? source.CreateDate).Date,
            cheque && !source.Vosoled ? source.VosolDateM?.Date : null, cheque && source.Vosoled,
            cheque && transfer != null, cheque && source.Vosoled ? source.VosolDateM?.Date : null,
            GetTarget(source.CustomerId, ExternalMappingTypeEnum.مشتری, "Party"),
            GetTarget(source.BankAccountId, ExternalMappingTypeEnum.حساب_بانکی, "BankAccount"),
            GetTarget(transfer?.CustomerId, ExternalMappingTypeEnum.مشتری, "Party"));
    }

    private async Task<IReadOnlyList<ExternalMapping>> GetSyncMappingsAsync(Accountancy source, CancellationToken ct)
    {
        var main = await repository.GetMappingsAsync(source.Id, source.CustomerId, source.BankAccountId, ct);
        if (source.PayType == PayTypeEnum.چکی && ActiveChequeTransfer(source)?.CustomerId is Guid payee && payee != source.CustomerId)
        {
            var payeeMappings = await repository.GetMappingsAsync(source.Id, payee, null, ct);
            main.AddRange(payeeMappings.Where(x => x.EntityId == payee && x.MapType == ExternalMappingTypeEnum.مشتری));
        }
        return main;
    }

    public async Task<object> PreviewSynchronizationAsync(DepositSynchronizationRequest request, CancellationToken ct)
    {
        var (source, mapping, destination) = await ResolveSynchronizationAsync(request, ct);
        var values = GetSyncSource(source, request.SoftwareId, await GetSyncMappingsAsync(source, ct));
        return await destination.GetDepositSyncPreviewAsync(ReadId(mapping), values, request.Action,
            request.SoftwareId, repository, ct);
    }

    public async Task<object> SubmitSynchronizationAsync(DepositSynchronizationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new SepidarTransferValidationException("ابتدا پیش‌نمایش همگام‌سازی را بررسی کنید.");
        await using var guard = await repository.LockAsync(request.Id, ct);
        var (source, mapping, destination) = await ResolveSynchronizationAsync(request, ct);
        var modifier = await users.GetCurrentExternalUserIdAsync(request.SoftwareId, destination, ct);
        var values = GetSyncSource(source, request.SoftwareId, await GetSyncMappingsAsync(source, ct));
        return await destination.SynchronizeDepositAsync(ReadId(mapping), values, request.Action, request.Token,
            modifier, request.SoftwareId, repository, ct);
    }

    private Task<(Accountancy Source, ExternalMapping Mapping, SepidarService Destination)> ResolveSynchronizationAsync(
        DepositSynchronizationRequest request, CancellationToken ct)
    {
        if (request.Action is not (DepositChequeAction.None or DepositChequeAction.Spend))
            throw new SepidarTransferValidationException("برای اصلاح عملیات بانکی چک، ابتدا آن را از سوابق حذف کنید.");
        return ResolveDepositMaintenanceAsync(new(request.Id, request.SoftwareId, request.MappingId,
            request.Action, true, null, null), ct);
    }
}
