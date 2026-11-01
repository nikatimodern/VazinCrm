using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Cashes.Enums;
using CRM.Core.Domain.Cashes.Repositories;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Cashes.Abstractions;
using CRM.Core.ApplicationServices.Cashes.Dtos;
using CRM.Core.ApplicationServices.Cashes.Services;

namespace CRM.Core.ApplicationServices.Cashes.Services;
public sealed class CashWorkflow(ICashRepository repository, IApplicationDates dates)
{
    public async Task ChangeCashStatus(CashStatusEnum status, Cash data, AppUser user, CancellationToken ct)
    {
        data.Status = status;
        data.StatusTxt = status.ToString().Replace("_", " ");
        data.ModifyDate = DateTime.Now;
        data.ModifyDateTxt = dates.PersianNow();
        data.UserModify = user.FullName;
        data.UserModifyID = user.Id;
        await SyncCashAccountancyDebit(data, user, ct);
    }

    public async Task AttachCashesToNewGroup(List<Cash> cashList, CashStatusEnum status, AppUser user, CancellationToken ct)
    {
        if (cashList == null || cashList.Count == 0)
            return;
        var group = new CashGroup
        {
            UserCreator = user.FullName,
            UserCreatorID = user.Id
        };
        repository.AddGroup(group);
        foreach (var cash in cashList)
        {
            await ChangeCashStatus(status, cash, user, ct);
            cash.CashGroup = group;
        }
    }

    public async Task SyncCashAccountancyDebit(Cash cash, AppUser user, CancellationToken ct)
    {
        if (cash == null)
            return;
        var acc = await repository.GetDebitAsync(cash.Id, ct);
        switch (cash.Status)
        {
            case CashStatusEnum.ثبت_موقت:
            {
                if (acc == null)
                {
                    acc = new Accountancy
                    {
                        CashId = cash.Id,
                        CashierId = Guid.Parse(cash.UserCreatorID),
                        InvoiceType = InvoiceTypeEnum.تنخواه,
                        InvoiceTypeTxt = nameof(InvoiceTypeEnum.تنخواه),
                        AccType = AcEnum.بدهکاری,
                        AccTypeTxt = nameof(AcEnum.بدهکاری),
                        debit = cash.TotalAmount,
                        UserCreatorID = user?.Id,
                        UserCreator = user?.FullName,
                        PayType = PayTypeEnum.نقدی,
                        PayTypeTxt = nameof(PayTypeEnum.نقدی),
                        Status = AccStatusEnum.ثبت_اولیه,
                    };
                    repository.AddDebit(acc);
                }
                else
                {
                    if (acc.debit != cash.TotalAmount)
                    {
                        acc.debit = cash.TotalAmount;
                        acc.UserModifyID = user?.Id;
                        acc.UserModify = user?.FullName;
                        acc.ModifyDate = DateTime.Now;
                        acc.ModifyDateTxt = dates.PersianNow();
                    }

                    acc.Status = AccStatusEnum.ثبت_اولیه;
                    acc.Accepted = false;
                    acc.AcceptedUser = null;
                    acc.AcceptedUserId = null;
                }

                break;
            }

            case CashStatusEnum.در_دست_تایید:
            {
                if (acc != null)
                {
                    acc.Status = AccStatusEnum.در_دست_تایید;
                    acc.Accepted = false;
                    acc.AcceptedUser = null;
                    acc.AcceptedUserId = null;
                }

                break;
            }

            case CashStatusEnum.تایید_شده:
            {
                if (acc != null)
                {
                    acc.Status = AccStatusEnum.تایید_شده;
                    acc.Accepted = true;
                    acc.AcceptedUser = user?.FullName;
                    acc.AcceptedUserId = user?.Id;
                }

                break;
            }
        }
    }
}
