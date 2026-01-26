using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Services.Factor;
using CRM.EndPoint.FrontEnd.Services;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Controllers
{
    public class ProductController : Controller
    {
        private readonly Service genService;
        private readonly FileSaver fileSaver;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly UserContextDb udb;
        private readonly SmsServices smsServ;
        private readonly UserManager<AppUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        public ProductController(Service genService, FileSaver fileSaver, DateTimeServices dtService, CRMContextDb db, UserContextDb udb, SmsServices smsServ, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this.genService = genService;
            this.fileSaver = fileSaver;
            this.dtService = dtService;
            this.db = db;
            this.udb = udb;
            this.smsServ = smsServ;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProduct()
        {
            var pr= await db.Products.Select(f=> new OpViewModelGuid() { id=f.Id,text=f.Title} ) .ToListAsync();
            return Ok(pr);
        }
    }
}
