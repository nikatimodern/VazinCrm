#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ReleaseNotes.Entities;
using CRM.Core.Domain.ReleaseNotes.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace CRM.Infrastructures.Sql.ReleaseNotes.Repositories;
public sealed class ReleaseNoteRepository(CRMContextDb db) : IReleaseNoteRepository
{
    private static IQueryable<ReleaseNote> Visible(IQueryable<ReleaseNote> query, ReleaseNoteReader reader, DateTime now, bool popup)
    {
        query = query.Where(x => x.PublishDate != null && x.PublishDate <= now &&
            (x.Status == ReleaseNoteStatus.Published || (!popup && x.Status == ReleaseNoteStatus.Archived && x.PublishDate <= x.ArchivedDate)) &&
            (x.Audience == ReleaseNoteAudience.All ||
             (x.Audience == ReleaseNoteAudience.Customers && reader.IsCustomer) ||
             (x.Audience == ReleaseNoteAudience.InternalUsers && reader.IsInternal) ||
             (x.Audience == ReleaseNoteAudience.SelectedRoles && x.Roles.Any(r => reader.Roles.Contains(r.RoleName)))));
        return popup ? query.Where(x => x.ShowPopup && !x.UserViews.Any(v => v.UserId == reader.UserId)) : query;
    }
    private static IQueryable<ReleaseNoteData> Projection(IQueryable<ReleaseNote> query) => query.Select(x => new ReleaseNoteData(
        x.Id, x.Version, x.Title, x.Description, x.Status, x.Audience, x.CreateDate, x.PublishDate, x.IsImportant, x.ShowPopup,
        Convert.ToBase64String(x.RowVersion), x.Roles.Select(r => r.RoleName).ToList(),
        x.Items.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).Select(i => new ReleaseNoteItemData(i.Id, i.Title, i.Description, i.Type, i.SortOrder, i.ImageUrl, i.LearnMoreUrl)).ToList()));
    private static Task<List<ReleaseNoteData>> Project(IQueryable<ReleaseNote> query, CancellationToken ct) => Projection(query).ToListAsync(ct);
    private static Version ParseVersion(string version) =>
        Version.TryParse(version.Split('-')[0], out var parsed) ? parsed : new Version(0, 0, 0);
    public async Task<List<ReleaseNoteData>> GetAdminListAsync(string? search, ReleaseNoteStatus? status, CancellationToken ct)
    {
        var q = db.ReleaseNotes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Title.Contains(search) || x.Version.Contains(search));
        if (status.HasValue) q = q.Where(x => x.Status == status);
        var notes = await Project(q, ct);

        return notes
            .OrderByDescending(x => Version.Parse(x.Version.Split('-')[0]))
            .ThenBy(x => x.Version.Contains('-'))
            .ThenByDescending(x => x.CreateDate)
            .ToList();
    }
    public async Task<ReleaseNoteData?> GetAsync(Guid id, CancellationToken ct) => (await Project(db.ReleaseNotes.AsNoTracking().Where(x => x.Id == id), ct)).SingleOrDefault();
    public async Task<List<ReleaseNoteData>> GetVisibleAsync(ReleaseNoteReader reader, bool unseenOnly, DateTime now, CancellationToken ct)
    {
        var releases = await Project(Visible(db.ReleaseNotes.AsNoTracking(), reader, now, unseenOnly), ct);
        return releases
            .OrderByDescending(x => unseenOnly && x.IsImportant)
            .ThenByDescending(x => ParseVersion(x.Version))
            .ThenBy(x => x.Version.Contains('-'))
            .ThenByDescending(x => x.PublishDate)
            .ThenByDescending(x => x.CreateDate)
            .ToList();
    }
    public async Task<string?> GetLatestVisibleVersionAsync(ReleaseNoteReader reader, DateTime now, CancellationToken ct) =>
        (await GetLatestVisibleVersionInfoAsync(reader, now, ct))?.Version;

    public async Task<ReleaseNoteVersionInfo?> GetLatestVisibleVersionInfoAsync(ReleaseNoteReader reader, DateTime now, CancellationToken ct)
    {
        // SQL string ordering treats 7.5.9 as newer than 7.5.10. Only retrieve the fields needed
        // and compare the numeric version components after the visibility filter is applied.
        var releases = await Visible(db.ReleaseNotes.AsNoTracking(), reader, now, false)
            .Select(x => new { x.Version, x.PublishDate, x.CreateDate })
            .ToListAsync(ct);

        var latest = releases
            .OrderByDescending(x => ParseVersion(x.Version))
            .ThenBy(x => x.Version.Contains('-')) // Stable release precedes a prerelease of the same version.
            .ThenByDescending(x => x.PublishDate)
            .ThenByDescending(x => x.CreateDate)
            .FirstOrDefault();

        return latest is null ? null : new ReleaseNoteVersionInfo(latest.Version, latest.PublishDate);
    }
    public async Task<(ReleaseNoteMutation Result, Guid Id)> SaveDraftAsync(ReleaseNote draft, Guid? id, byte[]? expectedVersion, CancellationToken ct)
    {
        var entity = draft;
        if (id.HasValue)
        {
            entity = await db.ReleaseNotes.Include(x => x.Items).Include(x => x.Roles).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return (ReleaseNoteMutation.NotFound, id.Value);
            if (entity.Status != ReleaseNoteStatus.Draft) return (ReleaseNoteMutation.Conflict, entity.Id);
            db.Entry(entity).Property(x => x.RowVersion).OriginalValue = expectedVersion!;
            db.ReleaseNoteItems.RemoveRange(entity.Items);
            var desiredRoles = draft.Roles.Select(x => x.RoleName).ToHashSet(StringComparer.Ordinal);
            db.ReleaseNoteRoles.RemoveRange(entity.Roles.Where(x => !desiredRoles.Contains(x.RoleName)));
            var existingRoles = entity.Roles.Select(x => x.RoleName).ToHashSet(StringComparer.Ordinal);
            foreach (var role in draft.Roles.Where(x => !existingRoles.Contains(x.RoleName)))
            {
                role.ReleaseNoteId = entity.Id;
                entity.Roles.Add(role);
                db.ReleaseNoteRoles.Add(role);
            }
            entity.Version = draft.Version; entity.Title = draft.Title; entity.Description = draft.Description;
            entity.Audience = draft.Audience; entity.PublishDate = draft.PublishDate;
            entity.ShowPopup = draft.ShowPopup; entity.IsImportant = draft.IsImportant;
            entity.Items = draft.Items;
            foreach (var item in entity.Items) item.ReleaseNoteId = entity.Id;
            db.ReleaseNoteItems.AddRange(entity.Items);
            foreach (var role in entity.Roles) role.ReleaseNoteId = entity.Id;
            // ویرایش آیتم‌ها هم باید هم‌زمانی با انتشار را بررسی کند.
            db.Entry(entity).Property(x => x.Title).IsModified = true;
        }
        else db.ReleaseNotes.Add(entity);
        try { await db.SaveChangesAsync(ct); return (ReleaseNoteMutation.Success, entity.Id); }
        catch (DbUpdateConcurrencyException) { return (ReleaseNoteMutation.Conflict, entity.Id); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627) && sql.Message.Contains("IX_ReleaseNotes_Version", StringComparison.Ordinal))
        { return (ReleaseNoteMutation.Conflict, entity.Id); }
    }
    public async Task<ReleaseNoteMutation> ChangeStatusAsync(Guid id, ReleaseNoteStatus target, DateTime now, CancellationToken ct)
    {
        var entity = await db.ReleaseNotes.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return ReleaseNoteMutation.NotFound;
        if (target == ReleaseNoteStatus.Published && entity.Status == ReleaseNoteStatus.Draft && entity.Items.Count > 0)
        { entity.Status = target; entity.PublishDate ??= now; }
        else if (target == ReleaseNoteStatus.Archived && entity.Status == ReleaseNoteStatus.Published)
        { entity.Status = target; entity.ArchivedDate = now; }
        else return ReleaseNoteMutation.Conflict;
        try { await db.SaveChangesAsync(ct); return ReleaseNoteMutation.Success; }
        catch (DbUpdateConcurrencyException) { return ReleaseNoteMutation.Conflict; }
    }
    public async Task<ReleaseNoteMutation> DeleteDraftAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.ReleaseNotes.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return ReleaseNoteMutation.NotFound;
        if (entity.Status != ReleaseNoteStatus.Draft) return ReleaseNoteMutation.Conflict;
        db.ReleaseNotes.Remove(entity);
        try { await db.SaveChangesAsync(ct); return ReleaseNoteMutation.Success; }
        catch (DbUpdateConcurrencyException) { return ReleaseNoteMutation.Conflict; }
    }
    public async Task<ReleaseNoteMutation> AcknowledgeAsync(ReleaseNoteReader reader, Guid[] ids, DateTime now, CancellationToken ct) =>
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            // تأیید تکراری نسخه مجاز هم باید موفق باشد.
            var eligible = await Visible(db.ReleaseNotes.AsNoTracking(), reader, now, false)
                .Where(x => ids.Contains(x.Id) && x.Status == ReleaseNoteStatus.Published && x.ShowPopup).Select(x => x.Id).ToArrayAsync(ct);
            if (eligible.Length != ids.Length) return ReleaseNoteMutation.Ineligible;
            foreach (var id in ids.OrderBy(x => x))
            {
                var viewId = Guid.NewGuid();
                // قفل بازه کلید از ثبت هم‌زمان رکورد تکراری جلوگیری می‌کند.
                await db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO [UserReleaseNotes] ([Id], [UserId], [ReleaseNoteId], [SeenDate])
SELECT {viewId}, {reader.UserId}, {id}, {now}
WHERE NOT EXISTS (SELECT 1 FROM [UserReleaseNotes] WITH (UPDLOCK, HOLDLOCK)
                  WHERE [UserId] = {reader.UserId} AND [ReleaseNoteId] = {id})", ct);
            }
            await transaction.CommitAsync(ct);
            return ReleaseNoteMutation.Success;
        });
}
