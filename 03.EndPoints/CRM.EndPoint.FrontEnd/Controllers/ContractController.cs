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
            var data = await db.Contracts.Include(f => f.Attachments).Where(f => f.Id == id).FirstOrDefaultAsync();
            return Ok(data);
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
            try
            {
                var ss = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                if (ModelState.IsValid)
                {
                    if (model.Attachments != null)
                        model.Attachments = fileSaver.SaveAttachments(model.Attachments, _AttachType.ContractAttach);
                    db.Contracts.Update(model);
                    await db.SaveChangesAsync();
                    return Ok(new { Status = true, Result = new { } });
                } else
                {
                    return Ok(new { Status = false, Result = new { } });
                }
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }
    }
}