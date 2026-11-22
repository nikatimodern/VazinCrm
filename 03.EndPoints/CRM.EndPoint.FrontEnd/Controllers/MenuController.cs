using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.IServices.Menu;
using CRM.EndPoint.FrontEnd.Models.Menu;
using CRM.Core.Domain.AppUsers.Entities;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize]
[Route("Menu")]
public sealed class MenuController(UserContextDb db, IMenuService menus) : Controller
{
    public sealed record FavoriteChange(string MenuId, bool Favorite);
    public sealed record FavoriteOrder(List<string> MenuIds);

    [HttpGet("Snapshot")]
    public IActionResult Snapshot() => PartialView("~/Views/Shared/Menu/_Menu.cshtml");

    [HttpPost("Favorite"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Favorite([FromBody] FavoriteChange request, CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.MenuId))
            return BadRequest();
        var allowed = await AllowedLinkIdsAsync();
        if (!allowed.Contains(request.MenuId)) return Forbid();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var favorite = await db.MenuFavorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.MenuId == request.MenuId, ct);
        if (request.Favorite && favorite == null)
        {
            var order = await db.MenuFavorites.Where(f => f.UserId == userId)
                .Select(f => (int?)f.SortOrder).MaxAsync(ct) ?? 0;
            db.MenuFavorites.Add(new MenuFavorite
            {
                UserId = userId, MenuId = request.MenuId, SortOrder = order + 1
            });
        }
        else if (!request.Favorite && favorite != null) db.MenuFavorites.Remove(favorite);
        await db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpPost("Reorder"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder([FromBody] FavoriteOrder request, CancellationToken ct)
    {
        if (request?.MenuIds is null || request.MenuIds.Count > 500) return BadRequest();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var items = await db.MenuFavorites.Where(f => f.UserId == userId)
            .OrderBy(f => f.SortOrder).ThenBy(f => f.MenuId).ToListAsync(ct);
        var mine = items.Select(f => f.MenuId).ToHashSet(StringComparer.Ordinal);
        var allowed = await AllowedLinkIdsAsync();
        var desired = request.MenuIds.Distinct(StringComparer.Ordinal).ToArray();
        if (desired.Any(id => !mine.Contains(id) || !allowed.Contains(id))) return BadRequest();

        var rank = desired.Concat(items.Select(f => f.MenuId).Where(id => !desired.Contains(id))).ToArray();
        for (var i = 0; i < rank.Length; i++)
            items.Single(f => f.MenuId == rank[i]).SortOrder = i + 1;
        await db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    private async Task<HashSet<string>> AllowedLinkIdsAsync()
    {
        var tree = await menus.GetMenusAsync(User);
        var allowed = new HashSet<string>(StringComparer.Ordinal);
        void Visit(IEnumerable<MenuItem> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.ShowInDesktop && !node.IsFavoritesGroup && node.Id != "releaseNotes"
                    && !string.IsNullOrWhiteSpace(node.Url)) allowed.Add(node.Id);
                if (!node.IsFavoritesGroup) Visit(node.SubItems);
            }
        }
        Visit(tree);
        return allowed;
    }
}
