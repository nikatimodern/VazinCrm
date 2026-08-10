using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.Contracts.Entities;
using CRM.EndPoint.FrontEnd.Services;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Controllers {
    public class ContractController : Controller {
        private readonly UserManager<AppUser> userManager;
        private readonly Service genService;
        private readonly FileSaver fileSaver;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;

        public ContractController(UserManager<AppUser> userManager, Service GenService, FileSaver fileSaver, DateTimeServices dtService,
            CRMContextDb db)
        {
            this.userManager = userManager;
            genService = GenService;
            this.fileSaver = fileSaver;
            this.dtService = dtService;
            this.db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetContract(Guid id)
        {
            var data = await db.Contracts.Include(f => f.Attachments).Where(f => f.Id == id).FirstOrDefaultAsync();
            return Ok(data);
        }

        public async Task<IActionResult> GetAllContract()
        {
            var data = await db.Contracts.ToListAsync();
            return PartialView(data);
        }

        public async Task<IActionResult> delContract(Guid id)
        {
            var data = await db.Contracts.Where(f => f.Id == id).FirstOrDefaultAsync();
            data.IsDeleted = true;
            await db.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> AddContract(Contract model)
        {
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
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException);
            }
        }
    }
}