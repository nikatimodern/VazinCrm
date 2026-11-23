using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.AccessCatalog.Repositories;
using Microsoft.Extensions.Configuration;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Deals;
using CRM.Core.Domain.Deals.Enums;
using CRM.Core.Domain.Deals.Repositories;
using CRM.Core.Domain.Querying;

namespace CRM.Core.ApplicationServices.OverdueDeals.Commands.UpdateOverdueFollowUp;
public sealed record FollowUpResult(int Status, string? Message = null);
public sealed record UpdateOverdueFollowUpCommand(Guid Id, string? FollowUpDate, DealStatusEnum Status, string? Note) : ICommand<FollowUpResult>;
public sealed class UpdateOverdueFollowUpCommandHandler(IDealRepository repository, CurrentActor actor, IApplicationDates dates,
    IAccessScopeService accessScopes, IConfiguration configuration) : ICommandHandler<UpdateOverdueFollowUpCommand, FollowUpResult>
{
    public async Task<FollowUpResult> HandleAsync(UpdateOverdueFollowUpCommand request, CancellationToken ct)
    {
        if (request.Id == Guid.Empty || request.Note?.Trim().Length > 4000)
            return new(400, "توضیحات پیگیری نباید بیشتر از ۴۰۰۰ نویسه باشد.");
        if (request.Status is not (DealStatusEnum.باز or DealStatusEnum.موفق or DealStatusEnum.ناموفق) || (request.Status != DealStatusEnum.باز && !string.IsNullOrWhiteSpace(request.FollowUpDate)))
            return new(400, "وضعیت سفارش یا تاریخ پیگیری مجدد معتبر نیست.");
        DateTime? followUpAt = null;
        if (!string.IsNullOrWhiteSpace(request.FollowUpDate))
        {
            followUpAt = dates.ParsePersian(request.FollowUpDate);
            if (!followUpAt.HasValue || followUpAt.Value.Date <= DateTime.Today)
                return new(400, "تاریخ پیگیری مجدد باید بعد از امروز باشد. برای ثبت بدون مهلت، تاریخ قبلی را پاک کنید.");
        }

        var current = await actor.FindAsync(ct);
        if (current == null)
            return new(401);
        if (bool.TryParse(configuration["AccessCatalog:EnforceDealPermissions"], out var enforceDeal) && enforceDeal)
        {
            if (!await accessScopes.CanAccessDealAsync(current.User.Id, "Deal.View", request.Id, ct) ||
                !await accessScopes.CanAccessDealAsync(current.User.Id, "Deal.ManageFollowUp", request.Id, ct) ||
                (request.Status != DealStatusEnum.باز &&
                 !await accessScopes.CanAccessDealAsync(current.User.Id, "Deal.ChangeStatus", request.Id, ct)))
                return new(403);
        }
        else if (!current.HasRole("مدیر فروش", "مدیر ارشد"))
            return new(403);
        var result = await repository.UpdateOverdueFollowUpAsync(request.Id, followUpAt, request.Status, request.Note, current.User.Id, current.User.FullName, ct);
        return result switch
        {
            OverdueFollowUpUpdateResult.Updated => new(200),
            OverdueFollowUpUpdateResult.HasFactors => new(409, "این سفارش فاکتور دارد و نمی‌توان وضعیت آن را ناموفق کرد."),
            _ => new(404, "سفارش معوقه یافت نشد یا دیگر باز نیست.")};
    }
}
