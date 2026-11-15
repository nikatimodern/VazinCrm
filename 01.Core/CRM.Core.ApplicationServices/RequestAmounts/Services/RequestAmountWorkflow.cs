using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.RequestAmounts.Enums;
using CRM.Core.Domain.RequestAmounts.Repositories;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Payments.Enums;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.ApplicationServices.RequestAmounts.Abstractions;
using CRM.Core.ApplicationServices.RequestAmounts.Services;
using CRM.Core.ApplicationServices.RequestAmounts.Dtos;

namespace CRM.Core.ApplicationServices.RequestAmounts.Services;
public sealed class RequestAmountWorkflow(IRequestAmountRepository repository, IApplicationDates dates, IApplicationAudit audit, IRequestAmountEffects effects)
{
    public async Task ChangeRequestAmountStatus(RequestAmount requestAmount, RequestStatusEnum newStatus, AppUser user, CancellationToken ct)
    {
        try
        {
            if (requestAmount == null)
                return;
            if (requestAmount.RequestStatus != newStatus)
            {
                var oldItem = await repository.GetAsync(requestAmount.Id, false, ct);
                requestAmount.RequestStatus = newStatus;
                requestAmount.RequestStatusTxt = newStatus.ToString().Replace("_", " ");
                if (newStatus == RequestStatusEnum.ثبت_اولیه)
                {
                    requestAmount.AcceptedAmount = 0;
                    requestAmount.AcceptedAmountPercent = null;
                }

                requestAmount.UserModify = user.FullName;
                requestAmount.UserModifyID = user.Id;
                requestAmount.ModifyDate = DateTime.Now;
                requestAmount.ModifyDateTxt = dates.PersianNow();
                await audit.LogAsync(oldItem, requestAmount, LogActionEnum.تغییر_وضعیت, user, ct);
            }

            if (requestAmount.RequestAmountType == RequestAmountTypeEnum.تنخواه)
            {
                await SyncCashAccountancyCredit(requestAmount, user, ct);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task SyncRequestAmountsPaymentState(Payment payment, AppUser user, bool changeStatus, CancellationToken ct)
    {
        try
        {
            var idsList = payment?.RequestAmountPayments?.Select(x => x.RequestAmountId).Distinct().ToList();
            var requestAmounts = await repository.GetByIdsAsync(idsList ?? [], true, ct);
            if (requestAmounts.Count == 0)
                return;
            foreach (var reqAmount in requestAmounts)
            {
                var newStatus = reqAmount.RequestStatus;
                if (changeStatus)
                {
                    var totalPaid = await repository.GetPaidAmountAsync(reqAmount.Id, ct);
                    newStatus = totalPaid > 0 ? RequestStatusEnum.پرداخت_شده : RequestStatusEnum.در_انتظار_پرداخت;
                }

                await ChangeRequestAmountStatus(reqAmount, newStatus, user, ct);
            }

            await repository.SaveAsync(ct);
            await effects.NotifyAsync(["مدیر ارشد", "کارشناس مالی"], ct);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task SyncCashAccountancyCredit(RequestAmount requestAmount, AppUser user, CancellationToken ct)
    {
        try
        {
            if (requestAmount == null)
                return;
            var acc = await repository.GetCashCreditAsync(requestAmount.Id, ct);
            if (requestAmount.RequestStatus == RequestStatusEnum.پرداخت_شده)
            {
                var totalPaid = await repository.GetPaidAmountAsync(requestAmount.Id, ct);
                if (acc == null)
                {
                    acc = new Accountancy
                    {
                        // Accountancy still stores the cashier's UserId; requests store the Cashier entity Id.
                        CashierId = requestAmount.TargetPersonId.HasValue
                            ? await repository.GetCashierUserIdAsync(requestAmount.TargetPersonId.Value, ct)
                            : null,
                        RequestAmountId = requestAmount.Id,
                        InvoiceType = InvoiceTypeEnum.تنخواه,
                        InvoiceTypeTxt = nameof(InvoiceTypeEnum.تنخواه),
                        AccType = AcEnum.واریزی,
                        AccTypeTxt = nameof(AcEnum.واریزی),
                        credit = totalPaid,
                        UserCreatorID = user.Id,
                        UserCreator = user.FullName,
                        Accepted = true,
                        AcceptedUserId = user.Id,
                        AcceptedUser = user.FullName,
                        PayType = PayTypeEnum.نقدی,
                        PayTypeTxt = nameof(PayTypeEnum.نقدی),
                        Status = AccStatusEnum.تایید_شده,
                    };
                    repository.AddCredit(acc);
                }
                else
                {
                    acc.credit = totalPaid;
                    acc.UserModifyID = user.Id;
                    acc.UserModify = user.FullName;
                    acc.ModifyDate = DateTime.Now;
                    acc.ModifyDateTxt = dates.PersianNow();
                }
            }
            else
            {
                if (acc != null)
                {
                    repository.RemoveCredit(acc);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task AddOrUpdatePayment(Payment model, AppUser user, bool isNewPayment, CancellationToken ct)
    {
        if (model == null)
            return;
        try
        {
            model.PaymentDate = dates.ParsePersian(model.PaymentDateTxt);
            if (isNewPayment)
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
                if (model.RequestAmountPayments?.Count == 1)
                    model.PaymentMode = PaymentModeEnum.تکی;
                repository.SavePayment(model, true);
                await audit.LogAsync(model, null, LogActionEnum.ثبت, user, ct);
            }
            else
            {
                var oldModel = await repository.GetPaymentAsync(model.Id, false, ct);
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dates.PersianNow();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;
                repository.SavePayment(model, false);
                await audit.LogAsync(oldModel, model, LogActionEnum.ویرایش, user, ct);
            }

            var attachList = effects.SaveAttachments(model.Attachments ?? []);
            foreach (var att in attachList ?? [])
            {
                att?.EntityId = model.Id;
            }

            repository.SaveAttachments(attachList ?? []);
            await repository.SaveAsync(ct);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<(bool IsValid, string Message)> ValidateRequestAmountsPayment(Payment model, CancellationToken ct)
    {
        if (model.RequestAmountPayments == null || model.RequestAmountPayments.Count == 0)
            return (false, "هیچ درخواست وجهی برای این پرداخت انتخاب نشده است.");
        if (model.ToAccountId == Guid.Empty)
            return (false, "حساب مقصد معتبر نیست.");

        var items = model.RequestAmountPayments;
        var requestAmountIds = items.Select(x => x.RequestAmountId).Distinct().ToList();
        if (requestAmountIds.Count != items.Count ||
            items.Any(x => x.RequestAmountId == Guid.Empty || x.Amount <= 0))
            return (false, "اقلام پرداخت تکراری یا دارای مبلغ نامعتبر هستند.");

        // Do not trust the request owner or bank options supplied by the browser.
        var linkedRequests = await repository.GetByIdsAsync(requestAmountIds, false, ct);
        if (linkedRequests.Count != requestAmountIds.Count)
            return (false, "یکی از درخواست‌های وجه یافت نشد.");

        var target = linkedRequests[0].TargetPersonId;
        var kind = linkedRequests[0].RequestAmountType.GetTargetKind();
        if (!target.HasValue || target.Value == Guid.Empty ||
            linkedRequests.Any(r => r.TargetPersonId != target || r.RequestAmountType.GetTargetKind() != kind))
            return (false, "تمام درخواست‌های یک پرداخت باید متعلق به یک شخص و از یک نوع مقصد باشند.");

        if (!await repository.IsDestinationBankAccountValidAsync(model.ToAccountId, target.Value, kind, ct))
            return (false, "حساب مقصد به شخص مربوط به درخواست وجه تعلق ندارد.");

        var balances = await repository.GetPaymentBalancesAsync(requestAmountIds, model.Id, ct);
        foreach (var item in items)
        {
            if (!balances.TryGetValue(item.RequestAmountId, out var requestAmount))
                return (false, "درخواست وجه برای پرداخت معتبر نیست.");

            if (requestAmount.PaidAmount + item.Amount > requestAmount.AcceptedAmount)
                return (false, "مبلغ پرداخت بیشتر از مبلغ تایید شده است.");
        }

        return (true, "");
    }
}
