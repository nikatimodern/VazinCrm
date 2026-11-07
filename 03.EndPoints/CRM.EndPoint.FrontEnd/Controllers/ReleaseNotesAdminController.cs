#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Dtos;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.ReleaseNotes.Entities;
using CRM.Core.ApplicationServices.ReleaseNotes.Commands.CreateReleaseNote;
using CRM.Core.ApplicationServices.ReleaseNotes.Commands.UpdateReleaseNote;
using CRM.Core.ApplicationServices.ReleaseNotes.Commands.PublishReleaseNote;
using CRM.Core.ApplicationServices.ReleaseNotes.Commands.ArchiveReleaseNote;
using CRM.Core.ApplicationServices.ReleaseNotes.Commands.DeleteReleaseNoteDraft;
using CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetReleaseNote;
using CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetReleaseNotes;
using CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetReleaseNoteAdminPage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Roles = AppRoles.Admin)]
[CRM.EndPoint.FrontEnd.Filters.ReleaseNoteAccess]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReleaseNotesAdminController(ICqrsDispatcher dispatcher) : Controller
{
    [HttpGet] public async Task<IActionResult> Index(CancellationToken ct) => View(await dispatcher.QueryAsync(new GetReleaseNoteAdminPageQuery(), ct));
    [HttpGet] public async Task<IActionResult> List(string? search, ReleaseNoteStatus? status, CancellationToken ct) => Json(await dispatcher.QueryAsync(new GetReleaseNotesQuery(search?.Trim(), status), ct));
    [HttpGet] public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var note = await dispatcher.QueryAsync(new GetReleaseNoteQuery(id), ct);
        return note is null ? NotFound() : Json(note);
    }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(1048576)]
    public async Task<IActionResult> Create([FromBody] ReleaseNoteDraft draft, CancellationToken ct) => !ModelState.IsValid ? BadRequest(new { message = "اطلاعات ارسالی معتبر نیست." }) : Result(await dispatcher.CommandAsync(new CreateReleaseNoteCommand(draft), ct));
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(1048576)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ReleaseNoteDraft draft, CancellationToken ct) => !ModelState.IsValid ? BadRequest(new { message = "اطلاعات ارسالی معتبر نیست." }) : Result(await dispatcher.CommandAsync(new UpdateReleaseNoteCommand(id, draft), ct));
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct) => Result(await dispatcher.CommandAsync(new PublishReleaseNoteCommand(id), ct));
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct) => Result(await dispatcher.CommandAsync(new ArchiveReleaseNoteCommand(id), ct));
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => Result(await dispatcher.CommandAsync(new DeleteReleaseNoteDraftCommand(id), ct));
    private IActionResult Result(ReleaseNoteResult result) => StatusCode(result.Status, result);
}
