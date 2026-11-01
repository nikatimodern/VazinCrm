using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Cartables.Abstractions;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFactors;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSupply;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Cartables.Repositories;
using CRM.EndPoint.FrontEnd.Models.Cartable;
using CRM.EndPoint.FrontEnd.Models.PreFactors;
using CRM.EndPoint.FrontEnd.Models.Factor;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;

namespace CRM.EndPoint.FrontEnd.Services.Cartables;
public sealed class CartableEffects(Service service, PreFactorService factors) : ICartableEffects
{
    public int ParseNumber(string value) => TextService.convertToInt(value ?? "");
    public Task FillPreFactorDetailsAsync(IList<PreFactor> data, CancellationToken ct) => factors.FillDetailsAsync(data);
    public async Task<object> GetEnvelopeAsync(AppUser user, int chats, CancellationToken ct) => new
    {
        countObj = await service.GetCartableCountObj(user),
        emailsCount = await service.GetEmailsNotSeenCount(Guid.Parse(user.Id)),
        chatsCount = chats
    };
}

public sealed class CartablePresentation(IViewRenderService views) : ICartablePresentation
{
    public async Task<object> PreFactorsAsync(CartablePreFactors data, CancellationToken ct) => new
    {
        count = data.Data.Count,
        html = await views.RenderToStringAsync("PreFactorAcceptedPartial", new PrefactorViewModel { data = data.Data, _Setting = data.Setting })
    };
    public async Task<object> FactorsAsync(CartableFactorData data, int passType, CancellationToken ct) => new
    {
        count = data.Factors?.Count ?? 0,
        html = await views.RenderToStringAsync("FactorPartial", new FactorsViewModel { Factors = data.Factors, _Setting = data.Setting }, new Dictionary<string, object> { { "PassType", passType } })
    };
    public async Task<object> SupplyAsync(CartableSupplyResult data, CancellationToken ct) => new
    {
        count = data.Data.PreFactors.Count,
        html = await views.RenderToStringAsync("GetPreFactorTaminExpert", new TaminViewModel { PreFactors = data.Data.PreFactors, Setting = data.Setting, UserId = data.UserId, IsAdmin = data.IsAdmin }),
        filterCounts = data.Data.FilterCounts
    };
    public async Task<object> FinancialAsync(CartableFinancialData data, CancellationToken ct) => new
    {
        count = data.Data.Count,
        html = await views.RenderToStringAsync("GetPreFactorFinancialExpert", data.Data.Select(x => new PreFactorFinancialExpertVModel { PreFactor = x.PreFactor, NotAcceptedFiAccCount = x.NotAcceptedFiAccCount, NotAcceptedMdAccCount = x.NotAcceptedMdAccCount, TotalAccCount = x.TotalAccCount, ReceiveAmount = x.ReceiveAmount }).ToList()),
        filterCounts = data.FilterCounts
    };
    public async Task<object> SalesAsync(CartableSalesData data, CancellationToken ct) => new
    {
        count = data.Data.Count,
        html = await views.RenderToStringAsync("GetPreFactorSalesAdmin", data.Data.Select(x => new PreFactorSalesAdminViewModel { PreFactor = x.PreFactor, ReceiveAmount = x.ReceiveAmount, SumTotalAmountTax = x.SumTotalAmountTax, PaymentAmount = x.PaymentAmount }).ToList()),
        filterCounts = data.FilterCounts
    };
}
