using System.Reflection;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.ReleaseNotes.Commands.AcknowledgeReleaseNotes;
using CRM.Core.ApplicationServices.ReleaseNotes.Dtos;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.Domain.ReleaseNotes.Entities;
using CRM.Core.Domain.ReleaseNotes.Repositories;
using CRM.Infrastructures.Sql;
using CRM.Infrastructures.Sql.ReleaseNotes.Repositories;
using Microsoft.EntityFrameworkCore;

var checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
var now = DateTime.UtcNow;
var user = new AppUser { Id = "real-user" };
IList<string> roles = new List<string> { AppRoles.Customer };
var users = Stub<IAccountUserRepository>.Create((method,args) => method switch {
    "FindByIdAsync" => Task.FromResult<AppUser?>(user), "GetRolesAsync" => Task.FromResult(roles), _ => throw new NotSupportedException(method) });
var context = Stub<IAccountRequestContext>.Create((method,args) => method == "get_UserId" ? user.Id : throw new NotSupportedException(method));
var access = new ReleaseNoteAccess(new CurrentActor(users,context));
var reader = await access.ReaderAsync(default);
Check(reader.IsCustomer && !reader.IsInternal, "Customer-only audience");
roles = new List<string> { AppRoles.Customer, AppRoles.SalesExpert };
reader = await access.ReaderAsync(default);
Check(reader.IsCustomer && reader.IsInternal, "Mixed-role audience");
roles = new List<string>(); reader = await access.ReaderAsync(default);
Check(!reader.IsCustomer && !reader.IsInternal, "Roleless authenticated audience");
var denied = false;
try { await access.DemandAdminAsync(default); } catch (UnauthorizedAccessException) { denied = true; }
Check(denied,"Administration denies non-admin");
roles = new List<string> { AppRoles.Admin }; await access.DemandAdminAsync(default);
var adminUsers = Stub<IAdminUserRepository>.Create((method,args) => method == "GetRolesAsync" ? Task.FromResult(new List<AdminRole> { new("1",AppRoles.Admin) }) : throw new NotSupportedException(method));
ReleaseNote? saved = null; Guid[]? acknowledged = null;
var repository = Stub<IReleaseNoteRepository>.Create((method,args) => method switch {
    "SaveDraftAsync" => Save((ReleaseNote)args![0]!),
    "AcknowledgeAsync" => Ack((ReleaseNoteReader)args![0]!, (Guid[])args[1]!),
    _ => throw new NotSupportedException(method) });
Task<(ReleaseNoteMutation, Guid)> Save(ReleaseNote note) { saved = note; return Task.FromResult((ReleaseNoteMutation.Success,note.Id)); }
Task<ReleaseNoteMutation> Ack(ReleaseNoteReader actor, Guid[] ids) { Check(actor.UserId == "real-user","Acknowledgement uses server identity"); acknowledged = ids; return Task.FromResult(ReleaseNoteMutation.Success); }
var service = new ReleaseNoteAdministration(access,repository,adminUsers);
ReleaseNoteDraft Draft() => new() { Version="7.0.5", Title="آزمون", Items=[new() { Title="تغییر",Type=ReleaseNoteItemType.NewFeature }] };
var draft = Draft(); Check((await service.SaveAsync(draft,null,default)).Status == 200 && saved != null,"Valid draft accepted");
draft = Draft(); draft.Version="7x0x5"; Check((await service.SaveAsync(draft,null,default)).Status == 400,"Invalid version rejected");
draft = Draft(); draft.Items.Clear(); Check((await service.SaveAsync(draft,null,default)).Status == 400,"Empty items rejected");
draft = Draft(); draft.Audience=ReleaseNoteAudience.SelectedRoles; draft.Roles=["made-up-role"]; Check((await service.SaveAsync(draft,null,default)).Status == 400,"Unknown role rejected");
draft.Roles=[AppRoles.Admin]; Check((await service.SaveAsync(draft,null,default)).Status == 200,"Actual selected role accepted");
draft = Draft(); draft.RowVersion="invalid"; Check((await service.SaveAsync(draft,Guid.NewGuid(),default)).Status == 400,"Invalid concurrency token rejected");
draft.RowVersion=null!; Check((await service.SaveAsync(draft,Guid.NewGuid(),default)).Status == 400,"Missing concurrency token rejected");
foreach (var url in new[] { "javascript:alert(1)", "data:text/html,test", "//evil.test", "/\\evil.test", "https://user:pass@example.com" }) Check(!ReleaseNoteAdministration.SafeUrl(url),"Unsafe URL rejected: " + url);
foreach (var url in new[] { "/help/new", "https://example.com/help", "" }) Check(ReleaseNoteAdministration.SafeUrl(url),"Safe URL accepted: " + url);
var ackHandler = new AcknowledgeReleaseNotesCommandHandler(access,repository);
Check((await ackHandler.HandleAsync(new([]),default)).Status == 400,"Empty acknowledgement rejected");
var id = Guid.NewGuid(); Check((await ackHandler.HandleAsync(new([id,id]),default)).Status == 200 && acknowledged!.Length == 1,"Duplicate acknowledgement IDs normalized");

var visible = typeof(ReleaseNoteRepository).GetMethod("Visible",BindingFlags.Static|BindingFlags.NonPublic)!;
IQueryable<ReleaseNote> Filter(IQueryable<ReleaseNote> source, ReleaseNoteReader actor, bool popup) => (IQueryable<ReleaseNote>)visible.Invoke(null,[source,actor,now,popup])!;
ReleaseNote Note(ReleaseNoteAudience audience=ReleaseNoteAudience.All) => new() { Title="x",Version="1.0.0",Status=ReleaseNoteStatus.Published,PublishDate=now.AddMinutes(-1),Audience=audience };
var all = new ReleaseNoteReader("u",[],false,false);
var customer = new ReleaseNoteReader("u",[AppRoles.Customer],true,false);
var employee = new ReleaseNoteReader("u",[AppRoles.SalesExpert],false,true);
Check(Filter(new[]{Note()}.AsQueryable(),all,true).Count()==1,"All includes roleless user");
Check(!Filter(new[]{Note(ReleaseNoteAudience.InternalUsers)}.AsQueryable(),customer,true).Any(),"Internal excludes customer-only");
Check(Filter(new[]{Note(ReleaseNoteAudience.InternalUsers)}.AsQueryable(),employee,true).Any(),"Internal includes employee");
Check(Filter(new[]{Note(ReleaseNoteAudience.Customers)}.AsQueryable(),customer,true).Any(),"Customers includes Customer role");
var selected = Note(ReleaseNoteAudience.SelectedRoles); selected.Roles.Add(new(){RoleName=AppRoles.SalesExpert});
Check(Filter(new[]{selected}.AsQueryable(),employee,true).Any() && !Filter(new[]{selected}.AsQueryable(),customer,true).Any(),"Selected roles enforced");
var future = Note(); future.PublishDate = now.AddDays(1);
var hidden = Note(); hidden.ShowPopup=false;
var seen = Note(); seen.UserViews.Add(new(){UserId="u"});
var draftNote = Note(); draftNote.Status=ReleaseNoteStatus.Draft;
var archived = Note(); archived.Status=ReleaseNoteStatus.Archived; archived.ArchivedDate=now;
var neverPublished = Note(); neverPublished.Status=ReleaseNoteStatus.Archived; neverPublished.PublishDate=now.AddMinutes(-1); neverPublished.ArchivedDate=now.AddDays(-1);
Check(!Filter(new[]{future,hidden,seen,draftNote,archived}.AsQueryable(),all,true).Any(),"Popup excludes future, disabled, seen, draft, archived");
Check(Filter(new[]{hidden,seen,archived}.AsQueryable(),all,false).Count()==3,"History includes seen, no-popup and published archive");
Check(!Filter(new[]{future,draftNote,neverPublished}.AsQueryable(),all,false).Any(),"History excludes future, draft and never-visible archive");
await using var db = new CRMContextDb(new DbContextOptionsBuilder<CRMContextDb>().UseSqlServer("Server=localhost;Database=DesignOnly;Integrated Security=True;TrustServerCertificate=True").Options);
var model = db.Model.FindEntityType(typeof(ReleaseNote))!;
Check(model.FindProperty("RowVersion")!.IsConcurrencyToken,"Publication concurrency protected");
Check(model.GetIndexes().Any(x=>x.IsUnique && x.Properties.Count==1 && x.Properties[0].Name=="Version"),"Unique version index");
var viewModel = db.Model.FindEntityType(typeof(UserReleaseNote))!;
Check(viewModel.GetIndexes().Any(x=>x.IsUnique && x.Properties.Select(p=>p.Name).SequenceEqual(new[]{"UserId","ReleaseNoteId"})),"Unique acknowledgement pair index");
Check(viewModel.GetForeignKeys().All(x=>x.DeleteBehavior==DeleteBehavior.Restrict),"Acknowledgements retained");
var query = Filter(db.ReleaseNotes.AsNoTracking(),employee,true);
var projection = typeof(ReleaseNoteRepository).GetMethod("Projection",BindingFlags.Static|BindingFlags.NonPublic)!;
var projected = (IQueryable<ReleaseNoteData>)projection.Invoke(null,[query])!;
var sql = projected.ToQueryString();
Check(sql.Contains("UserReleaseNotes") && sql.Contains("ReleaseNoteRoles"),"Audience and unseen projection translates to SQL without connection");
Check(!db.Database.HasPendingModelChanges(),"Migration snapshot matches model");
Console.WriteLine($"{checks} checks passed; no database connection opened.");
if (args.Contains("--browser")) await BrowserChecks.RunAsync();

public class Stub<T> : DispatchProxy where T : class
{
    public Func<string,object?[]?,object?> Call = null!;
    public static T Create(Func<string,object?[]?,object?> callback) { var instance = DispatchProxy.Create<T,Stub<T>>(); ((Stub<T>)(object)instance).Call = callback; return instance; }
    protected override object? Invoke(MethodInfo? method, object?[]? args) => Call(method!.Name,args);
}
