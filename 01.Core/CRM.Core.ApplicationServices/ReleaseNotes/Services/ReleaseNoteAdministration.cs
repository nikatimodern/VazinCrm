using System.Text.RegularExpressions;
using CRM.Core.ApplicationServices.ReleaseNotes.Dtos;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.Domain.ReleaseNotes.Entities;
using CRM.Core.Domain.ReleaseNotes.Repositories;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Services;
public sealed class ReleaseNoteAdministration(ReleaseNoteAccess access, IReleaseNoteRepository repository, IAdminUserRepository users)
{
    public async Task<ReleaseNoteResult> SaveAsync(ReleaseNoteDraft input, Guid? id, CancellationToken ct)
    {
        await access.DemandAdminAsync(ct);
        if (input is null || !Regex.IsMatch(input.Version ?? "", @"^\d{1,4}\.\d{1,4}\.\d{1,4}(?:-[A-Za-z0-9.-]{1,32})?$"))
            return new(400, "شماره نسخه باید مانند 7.0.5 باشد.");
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200 || input.Description?.Length > 4000 ||
            !Enum.IsDefined(input.Audience) || input.Items is null || input.Items.Count is < 1 or > 100 || input.Roles is null || input.Roles.Count > 100)
            return new(400, "عنوان، مخاطبان و حداقل یک تغییر معتبر لازم است.");
        if (input.Items.Any(i => i is null || string.IsNullOrWhiteSpace(i.Title) || i.Title.Length > 200 || i.Description?.Length > 4000 ||
            !Enum.IsDefined(i.Type) || i.SortOrder < 0 || !SafeUrl(i.ImageUrl) || !SafeUrl(i.LearnMoreUrl)))
            return new(400, "عنوان یا لینک یکی از تغییرات معتبر نیست.");
        var roles = input.Audience == ReleaseNoteAudience.SelectedRoles ? input.Roles.Distinct(StringComparer.Ordinal).ToList() : [];
        if (input.Audience == ReleaseNoteAudience.SelectedRoles)
        {
            var valid = (await users.GetRolesAsync(ct)).Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
            if (roles.Count == 0 || roles.Any(r => !valid.Contains(r))) return new(400, "حداقل یک نقش معتبر انتخاب کنید.");
        }
        byte[]? expected = null;
        if (id.HasValue)
        {
            try { expected = Convert.FromBase64String(input.RowVersion ?? ""); }
            catch (FormatException) { return new(400, "نسخه ویرایش معتبر نیست؛ صفحه را تازه‌سازی کنید."); }
            if (expected.Length != 8) return new(400, "نسخه ویرایش معتبر نیست.");
        }
        var note = new ReleaseNote { Version = input.Version!.Trim(), Title = input.Title.Trim(), Description = input.Description?.Trim(),
            Audience = input.Audience, PublishDate = input.PublishDate?.UtcDateTime, IsImportant = input.IsImportant, ShowPopup = input.ShowPopup };
        note.Roles = roles.Select(r => new ReleaseNoteRole { ReleaseNoteId = note.Id, RoleName = r }).ToList();
        note.Items = input.Items.OrderBy(i => i.SortOrder).Select((i, index) => new ReleaseNoteItem {
            ReleaseNoteId = note.Id, Title = i.Title.Trim(), Description = i.Description?.Trim(), Type = i.Type, SortOrder = index,
            ImageUrl = string.IsNullOrWhiteSpace(i.ImageUrl) ? null : i.ImageUrl.Trim(), LearnMoreUrl = string.IsNullOrWhiteSpace(i.LearnMoreUrl) ? null : i.LearnMoreUrl.Trim() }).ToList();
        var result = await repository.SaveDraftAsync(note, id, expected, ct);
        return Map(result.Result, result.Id);
    }
    public static bool SafeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        value = value.Trim();
        if (value.Length > 2048 || value.Any(char.IsControl) || value.Contains('\\')) return false;
        if (value.StartsWith('/') && !value.StartsWith("//")) return true;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http") && string.IsNullOrEmpty(uri.UserInfo);
    }
    public static ReleaseNoteResult Map(ReleaseNoteMutation status, Guid? id = null) => status switch {
        ReleaseNoteMutation.Success => new(200, Id:id),
        ReleaseNoteMutation.NotFound => new(404, "انتشار پیدا نشد."),
        ReleaseNoteMutation.Ineligible => new(403, "یک یا چند انتشار برای تأیید مجاز نیست."),
        _ => new(409, "نسخه تکراری است یا وضعیت انتشار تغییر کرده؛ اطلاعات را تازه‌سازی کنید.") };
}
