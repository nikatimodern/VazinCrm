using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Cartable;
using CRM.EndPoint.FrontEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartablePreFactors;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFactors;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSupply;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFinancial;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSales;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableOwnPreFactors;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableModalFactors;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableEnvelope;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartablePreFactorsHtml;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFactorsHtml;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableModalFactorsHtml;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSupplyHtml;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFinancialHtml;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSalesHtml;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize]
public class CartableController(ICqrsDispatcher dispatcher) : Controller
{
    [Authorize(Policy = "NotCustomerOnly")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var data = await dispatcher.QueryAsync(new GetWorkspacePageQuery("Cartable"), ct);
        ViewBag.SubTitle = "کارتابل من";
        ViewBag.Head = "Cartable";
        return View(data);
    }

    [Authorize(Policy = "NotCustomerOnly"), HttpGet]
    public async Task<IActionResult> GetCountEnvelope(CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCartableEnvelopeQuery(), ct));
    [HttpPost, Authorize(Roles = "کارشناس فروش ,مدیر ارشد ,مدیر مالی, کارشناس مالی,مدیر فروش,کارشناس تامین")]
    public async Task<IActionResult> PreFactorAcceptedPartial([FromBody] FilterVModel model, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCartablePreFactorsHtmlQuery(model.ToApplicationFilter()), ct));
    [HttpPost, Authorize]
    public async Task<IActionResult> PrefactorExportToExcel([FromBody] FilterVModel model, CancellationToken ct)
    {
        var preFactors = (await dispatcher.QueryAsync(new GetCartablePreFactorsQuery(model.ToApplicationFilter()), ct)).Data;
        var columns = new List<ExcelColumnConfig<PreFactor>>
        {
            new()
            {
                Header = "شماره سریال",
                ValueSelector = x => x.SerialNumber
            },
            new()
            {
                Header = "نوع",
                ValueSelector = x => x.OfficialType == 1 ? "رسمی" : "غیر رسمی"
            },
            new()
            {
                Header = "تاریخ صدور",
                ValueSelector = x => TextService.ToEnglishNumber(x.Date)
            },
            new()
            {
                Header = "تاریخ اعتبار",
                ValueSelector = x => TextService.ToEnglishNumber(x.ValidityDade)
            },
            new()
            {
                Header = "جمع کل",
                ValueSelector = x => x.SumTotalAmount,
                NumberFormat = "#,##0_-ريال"
            },
            new()
            {
                Header = "جمع کل با ارزش افزوده",
                ValueSelector = x => x.PaymentAmount,
                NumberFormat = "#,##0_-ريال"
            },
            new()
            {
                Header = "مشتری",
                ValueSelector = x => $"{x.Customer} {x.AgentName}"},
            new()
            {
                Header = "کارشناس فروش",
                ValueSelector = x => x.UserCreator
            },
            new()
            {
                Header = "وضعیت",
                ValueSelector = x => x.PftypeTxt
            },
            new()
            {
                Header = "تایید",
                ValueSelector = x => x.Accepted == true ? "بله" : "خیر"
            },
            new()
            {
                Header = "صادراتی",
                ValueSelector = x => x.Export == true ? "بله" : "خیر"
            }
        };
        return ExcelExportService.ExportToExcel(preFactors, columns, "PreFactors");
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> FactorExportToExcel([FromBody] FactorVModel model, CancellationToken ct)
    {
        var factors = (await dispatcher.QueryAsync(new GetCartableFactorsQuery(model.FModel.ToApplicationFilter(), model.CustomerId), ct)).Factors;
        var columns = new List<ExcelColumnConfig<Factor>>
        {
            new()
            {
                Header = "شماره سریال",
                ValueSelector = x => x.SerialNumber
            },
            new()
            {
                Header = "نوع",
                ValueSelector = x => x.OfficialType == 1 ? "رسمی" : "غیر رسمی"
            },
            new()
            {
                Header = "تاریخ",
                ValueSelector = x => TextService.ToEnglishNumber(x.Date)
            },
            new()
            {
                Header = "شماره پیش فاکتور",
                ValueSelector = x => x.PfactorSerialNumber
            },
            new()
            {
                Header = "جمع کل",
                ValueSelector = x => x.SumTotalAmount,
                NumberFormat = "#,##0_-ريال"
            },
            new()
            {
                Header = "جمع کل با ارزش افزوده",
                ValueSelector = x => x.PaymentAmount,
                NumberFormat = "#,##0_-ريال"
            },
            new()
            {
                Header = "مشتری",
                ValueSelector = x => $"{x.Customer} {x.AgentName}"},
            new()
            {
                Header = "کارشناس فروش",
                ValueSelector = x => x.SelerFullName
            },
            new()
            {
                Header = "شماره فاکتور مالی",
                ValueSelector = x => x.SnFinancial
            },
            new()
            {
                Header = "وضعیت",
                ValueSelector = x => x.statusTxt
            },
            new()
            {
                Header = "صادراتی",
                ValueSelector = x => x.Export == true ? "بله" : "خیر"
            },
            new()
            {
                Header = "ثبت شده توسط",
                ValueSelector = x => x.UserCreator
            }
        };
        return ExcelExportService.ExportToExcel(factors, columns, "PreFactors");
    }

    [HttpGet, Authorize(Roles = "مدیر ارشد , کارشناس تامین , مدیر تامین")]
    public async Task<IActionResult> GetPreFactorTaminExpert(string serVal, int type, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCartableSupplyHtmlQuery(serVal, type), ct));
    [HttpGet, Authorize(Roles = "مدیر ارشد , مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> GetPreFactorFinancialExpert(string serVal, int type = 1, CancellationToken ct = default) => Ok(await dispatcher.QueryAsync(new GetCartableFinancialHtmlQuery(serVal, type), ct));
    [HttpGet]
    public async Task<IActionResult> GetSelesExpertPartial(CancellationToken ct) => PartialView(await dispatcher.QueryAsync(new GetCartableOwnPreFactorsQuery(), ct));
    [HttpGet, Authorize(Roles = "مدیر ارشد , مدیر فروش")]
    public async Task<IActionResult> GetPreFactorSalesAdmin(string serVal, int type, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCartableSalesHtmlQuery(serVal, type), ct));
    [HttpPost]
    public async Task<IActionResult> FactorPartial([FromBody] FactorVModel model, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCartableFactorsHtmlQuery(model.FModel.ToApplicationFilter(), model.CustomerId, model.PassType), ct));
    [HttpGet]
    public async Task<IActionResult> GetAllFactorsPartialModal(Guid id, int type, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCartableModalFactorsHtmlQuery(id, type), ct));
}
