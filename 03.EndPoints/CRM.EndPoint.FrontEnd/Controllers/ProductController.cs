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
using CRM.EndPoint.FrontEnd.Services.SmsServices;

namespace CRM.EndPoint.FrontEnd.Controllers {
    public class ProductController : Controller {
        private readonly CRMContextDb db;

        public ProductController(CRMContextDb db)
        {
            this.db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProduct()
        {
            var pr = await db.Products.Select(f => new SelectOptionIdDto() {
                id = f.Id,
                text = f.Title
            }).ToListAsync();
            return Ok(pr);
        }
    }
}