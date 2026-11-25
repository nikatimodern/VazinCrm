using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.PreFactors.Repositories;
using CRM.EndPoint.FrontEnd.Models.Setting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Roles = "مدیر ارشد")]
[AutoValidateAntiforgeryToken]
public sealed class PrefactorTermsController(IPrefactorTermsRepository repository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? editId, CancellationToken ct)
    {
        var terms = await repository.GetAsync(ct);
        var selected = terms.FirstOrDefault(x => x.Id == editId);
        if (editId.HasValue && selected == null) return NotFound();

        var nextOrder = terms.Count == 0 ? 1 : Math.Max(1, terms.Max(x => x.Order));
        if (terms.Count > 0 && nextOrder < int.MaxValue) nextOrder++;

        return View(new PrefactorTermsPageModel
        {
            Terms = terms,
            Input = selected == null
                ? new PrefactorTermInput { Order = nextOrder }
                : new PrefactorTermInput { Id = selected.Id, Order = selected.Order, Text = selected.Text }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save([Bind(Prefix = "Input")] PrefactorTermInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View("Index", new PrefactorTermsPageModel { Terms = await repository.GetAsync(ct), Input = input });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Forbid();

        if (!await repository.SaveAsync(input.Id, input.Order, input.Text.Trim(), userId, ct))
            return NotFound();

        TempData["PrefactorTermsMessage"] = "بند پیش‌فاکتور ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reorder([FromBody] PrefactorTermsReorderInput input, CancellationToken ct)
    {
        if (input?.OriginalIds == null || input.OrderedIds == null ||
            input.OriginalIds.Length != input.OrderedIds.Length ||
            input.OriginalIds.Distinct().Count() != input.OriginalIds.Length ||
            input.OrderedIds.Distinct().Count() != input.OrderedIds.Length)
            return BadRequest("ترتیب بندها نامعتبر است.");

        if (!await repository.ReorderAsync(input.OriginalIds, input.OrderedIds, ct))
            return Conflict("فهرست بندها تغییر کرده است. صفحه را به‌روزرسانی کنید.");

        return Ok(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmDelete(int id, CancellationToken ct)
    {
        var term = (await repository.GetAsync(ct)).FirstOrDefault(x => x.Id == id);
        return term == null ? NotFound() : View(term);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await repository.DeleteAsync(id, ct)) return NotFound();
        TempData["PrefactorTermsMessage"] = "بند پیش‌فاکتور حذف شد.";
        return RedirectToAction(nameof(Index));
    }
}
