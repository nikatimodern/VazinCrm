#nullable enable
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Commands.AcknowledgeReleaseNotes;
using CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetUnseenReleaseNotes;
using CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetReleaseNoteHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize]
[CRM.EndPoint.FrontEnd.Filters.ReleaseNoteAccess]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReleaseNotesController(ICqrsDispatcher dispatcher) : Controller
{
    [HttpGet] public async Task<IActionResult> History(CancellationToken ct) => View(await dispatcher.QueryAsync(new GetReleaseNoteHistoryQuery(), ct));
    [HttpGet] public async Task<IActionResult> HistoryData(CancellationToken ct)
    {
        var notes = await dispatcher.QueryAsync(new GetReleaseNoteHistoryQuery(), ct);
        return Json(notes.Select(x => new { x.Id, x.Version, x.Title, x.Description, x.PublishDate, x.IsImportant, x.Items }));
    }
    [HttpGet] public async Task<IActionResult> Unseen(CancellationToken ct)
    {
        var notes = await dispatcher.QueryAsync(new GetUnseenReleaseNotesQuery(), ct);
        // نقش‌ها و نسخه هم‌زمانی به مرورگر کاربر ارسال نمی‌شوند.
        return Json(notes.Select(x => new { x.Id, x.Version, x.Title, x.Description, x.PublishDate, x.IsImportant, x.Items }));
    }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(32768)]
    public async Task<IActionResult> Acknowledge([FromBody] AcknowledgeInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "اطلاعات ارسالی معتبر نیست." });
        var result = await dispatcher.CommandAsync(new AcknowledgeReleaseNotesCommand(input?.Ids!), ct);
        return StatusCode(result.Status, result);
    }
    public sealed record AcknowledgeInput(List<Guid> Ids);
}
