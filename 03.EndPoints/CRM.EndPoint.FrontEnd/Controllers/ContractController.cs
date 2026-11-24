using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.Contracts.Entities;
using CRM.EndPoint.FrontEnd.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using CRM.EndPoint.FrontEnd.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using CRM.Infrastructures.Sql;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize(Policy = "NotCustomerOnly")]
    public class ContractController : Controller {
        private readonly UserManager<AppUser> userManager;
        private readonly Service genService;
        private readonly FileSaver fileSaver;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly DocumentAccessPolicy access;

        public ContractController(UserManager<AppUser> userManager, Service GenService, FileSaver fileSaver, DateTimeServices dtService,
            CRMContextDb db, DocumentAccessPolicy access)
        {
            this.userManager = userManager;
            genService = GenService;
            this.fileSaver = fileSaver;
            this.dtService = dtService;
            this.db = db;
            this.access = access;
        }

        public async Task<IActionResult> Index()
        {
            if (!await access.HasViewAsync("Contract", HttpContext.RequestAborted)) return Forbid();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetContract(Guid id)
        {
            if (!await access.CanAsync("Contract", "View", id, HttpContext.RequestAborted)) return Forbid();
            var data = await db.Contracts.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, HttpContext.RequestAborted);
            if (data == null) return NotFound();
            data.Attachments = await db.Attachments.AsNoTracking().Where(a => a.EntityId == id &&
                a._AttachType == _AttachType.ContractAttach).ToListAsync(HttpContext.RequestAborted);
            ViewBag.CanEdit = await access.AllowsRowAsync(data, "Edit", HttpContext.RequestAborted);
            return Ok(new { data.Id, data.CTitle, data.Employer, data.Contractor, data.fromDate, data.fromDateSh, data.toDate, data.toDateSh, data.description, data.Attachments, canEdit = (bool)ViewBag.CanEdit });
        }

        public async Task<IActionResult> GetAllContract()
        {
            if (!await access.HasViewAsync("Contract", HttpContext.RequestAborted)) return Forbid();
            var data = await (await access.ApplyAsync(db.Contracts.AsNoTracking(), "View", HttpContext.RequestAborted)).ToListAsync(HttpContext.RequestAborted);
            return PartialView(data);
        }

        public async Task<IActionResult> delContract(Guid id)
        {
            if (!await access.CanAsync("Contract", "Delete", id, HttpContext.RequestAborted)) return Forbid();
            var data = await db.Contracts.Where(f => f.Id == id).FirstOrDefaultAsync();
            if (data == null) return NotFound();
            data.IsDeleted = true;
            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> AddContract(Contract model)
        {
            if (model == null) return BadRequest();
            var ct = HttpContext.RequestAborted;
            if (!await access.HasViewAsync("Contract", ct)) return Forbid();
            if (model.Id == Guid.Empty)
            {
                if (!await access.HasAsync("Contract", "Create", ct)) return Forbid();
            }
            else if (!await access.CanAsync("Contract", "Edit", model.Id, ct)) return Forbid();
            if (!ModelState.IsValid) return Ok(new { Status = false, Result = new { } });
            var user = await userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            try
            {
                var create = model.Id == Guid.Empty;
                var existing = create ? null : await db.Contracts.FirstOrDefaultAsync(c => c.Id == model.Id, ct);
                if (!create && existing == null) return NotFound();
                var target = existing ?? new Contract
                {
                    Id = Guid.NewGuid(), UserCreatorID = user.Id, UserCreator = user.FullName
                };
                var storedAttachments = await db.Attachments.Where(a => a.EntityId == target.Id &&
                    a._AttachType == _AttachType.ContractAttach).ToListAsync(ct);
                var submitted = model.Attachments ?? Array.Empty<CRM.Core.Domain.Attachments.Entities.Attachment>();
                if (submitted.Any(a => a == null || (a.Id != Guid.Empty && !storedAttachments.Any(x => x.Id == a.Id))) ||
                    submitted.Where(a => a.Id != Guid.Empty).GroupBy(a => a.Id).Any(g => g.Count() > 1))
                    return BadRequest();

                target.CTitle = model.CTitle;
                target.Employer = model.Employer;
                target.Contractor = model.Contractor;
                target.fromDate = model.fromDate;
                target.fromDateSh = model.fromDateSh;
                target.toDate = model.toDate;
                target.toDateSh = model.toDateSh;
                target.description = model.description;
                target.UserModifyID = user.Id;
                target.UserModify = user.FullName;
                target.ModifyDate = DateTime.Now;
                target.ModifyDateTxt = dtService.GetDateTimePersian();

                foreach (var attachment in submitted.Where(a => a.Id != Guid.Empty && a.IsDeleted))
                {
                    var stored = storedAttachments.Single(a => a.Id == attachment.Id);
                    stored.IsDeleted = true;
                    stored.UserModifyID = user.Id;
                    stored.UserModify = user.FullName;
                    stored.ModifyDate = DateTime.Now;
                    stored.ModifyDateTxt = target.ModifyDateTxt;
                }
                var uploads = submitted.Where(a => a.Id == Guid.Empty && !a.IsDeleted).ToList();
                foreach (var upload in uploads)
                {
                    upload.EntityId = target.Id;
                    upload.UserCreatorID = user.Id;
                    upload.UserCreator = user.FullName;
                    upload.UserModifyID = user.Id;
                    upload.UserModify = user.FullName;
                }
                db.Attachments.AddRange(fileSaver.SaveAttachments(uploads, _AttachType.ContractAttach));
                if (create) db.Contracts.Add(target);
                await db.SaveChangesAsync(ct);
                return Ok(new { Status = true, Result = new { } });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception)
            {
                return BadRequest();
            }
        }
    }
}