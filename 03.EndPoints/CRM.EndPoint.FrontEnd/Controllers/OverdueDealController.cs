using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
using CRM.Core.ApplicationServices.OverdueDeals.Queries.GetOverdueDeals;
using CRM.Core.ApplicationServices.OverdueDeals.Queries.GetOverdueDealDetails;
using CRM.Core.ApplicationServices.OverdueDeals.Commands.UpdateOverdueFollowUp;
using CRM.Core.Domain.Deals.Enums;
using CRM.EndPoint.FrontEnd.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
[Authorize(Roles = "مدیر فروش,مدیر ارشد,مدیر عامل,کارشناس فروش")]
public sealed class OverdueDealController(ICqrsDispatcher requests) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await requests.QueryAsync(new GetWorkspacePageQuery("OverdueDeal"), ct));
    [HttpPost]
    public async Task<IActionResult> GetOverdueDeals([FromBody] FilterVModel model, DateTime? startDate, DateTime? endDate, string sortBy = "createDate", string sortDirection = "desc", bool withFollowUp = false, CancellationToken ct = default) => Ok(await requests.QueryAsync(new GetOverdueDealsQuery(model.ToApplicationFilter(), startDate, endDate, sortBy, sortDirection, withFollowUp), ct));
    [HttpGet]
    public async Task<IActionResult> GetOverdueDealDetails(Guid id, CancellationToken ct)
    {
        var result = await requests.QueryAsync(new GetOverdueDealDetailsQuery(id), ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost, Authorize(Roles = "مدیر فروش,مدیر ارشد")]
    public async Task<IActionResult> UpdateFollowUp([FromBody] OverdueFollowUpRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { message = "توضیحات پیگیری نباید بیشتر از ۴۰۰۰ نویسه باشد." });
        var result = await requests.CommandAsync(new UpdateOverdueFollowUpCommand(request.Id, request.FollowUpDate, request.Status, request.Note), cancellationToken);
        return result.Message == null ? StatusCode(result.Status) : StatusCode(result.Status, new { message = result.Message });
    }
}

public sealed class OverdueFollowUpRequest
{
    public Guid Id { get; set; }
    public string FollowUpDate { get; set; }
    public DealStatusEnum Status { get; set; }
    public string Note { get; set; }
}
