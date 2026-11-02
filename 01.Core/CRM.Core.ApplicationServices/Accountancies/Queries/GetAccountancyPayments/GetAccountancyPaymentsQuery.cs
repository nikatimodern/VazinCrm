using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.Cartables.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Accountancies.Abstractions;
using CRM.Core.ApplicationServices.Accountancies.Dtos;
using CRM.Core.ApplicationServices.Accountancies.Services;

namespace CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountancyPayments;
public sealed record GetAccountancyPaymentsQuery(int Type, Guid Id, int PassType, bool DepositsOnly = false, bool RealizedDepositsOnly = false, bool ReturnsOnly = false) : IQuery<object>;
public sealed class GetAccountancyPaymentsQueryHandler(IAccountancyRepository repository, CurrentActor actor, IAccountancyEffects effects) : IQueryHandler<GetAccountancyPaymentsQuery, object>
{
    public async Task<object> HandleAsync(GetAccountancyPaymentsQuery request, CancellationToken ct)
    {
        var type = request.Type;
        var id = request.Id;
        var passType = request.PassType;
        var current = await actor.GetAsync(ct);
        var user = current.User;
        var roles = current.Roles;
        Guid? customerId = null;
        PreFactor pf = null;
        Deal deal = null;
        var showAcceptPfBtn = false;
        var showChangeStatusAllBtn = false;
        var model = new PaymentPartialData
        {
            PassType = passType,
            UserId = user.Id
        };
        var data = await repository.GetPaymentsAsync(type, id, request.DepositsOnly, request.RealizedDepositsOnly, request.ReturnsOnly, ct);
        pf = data.PreFactor;
        deal = data.Deal;
        customerId = type == 1 ? pf?.CustomerId : type == 3 ? deal?.CustomerId : null;
        var acc = data.Items;
        model.Items = acc;
        if (acc.Count > 0)
        {
            PaymentPermissions.Apply(model, roles, user);
            if (type == 3) // deal
            {
                model.IsCreatorParent = deal?.UserCreatorID == user.Id;
            }
            else if (type == 1) // pf
            {
                model.IsCreatorParent = pf?.UserCreatorID == user.Id;
                showAcceptPfBtn = (model.IsAdmin || model.IsFinancial) && pf?.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مالی && pf.AcceptedFinancial != true && acc.All(f => f.Status == AccStatusEnum.تایید_شده);
                var isInvalid = pf?.Pftype is (int)PreFactorEnum.فاقد_اعتبار or (int)PreFactorEnum.بسته_شده or (int)PreFactorEnum.حذف_شده or (int)PreFactorEnum.برگشت_خورده_از_طرف_مدیر_فروش;
                var hasPendingOrRejected = acc.Any(f => f.Status is AccStatusEnum.ثبت_اولیه or AccStatusEnum.رد_شده);
                showChangeStatusAllBtn = passType != 5 && (model.IsCreatorParent || model.IsAdmin || model.IsSalesAdmin) && hasPendingOrRejected && !isInvalid;
            }
        }

        return new
        {
            data = new
            {
                showAcceptPfBtn,
                showChangeStatusAllBtn,
                customerId
            },
            html = await effects.RenderPaymentsAsync(model, ct)
        };
    }
}
