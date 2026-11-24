using CRM.EndPoint.FrontEnd.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models;
using CRM.Infrastructures.Sql;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize]
    public class ProductController : Controller {
        private readonly CRMContextDb db;

        public ProductController(CRMContextDb db)
        {
            this.db = db;
        }

        [HttpGet]
        [CatalogPermission("Product", "View")]
        public async Task<IActionResult> GetAllProduct()
        {
            var pr = await db.Products.AsNoTracking().Where(f => !f.IsDeleted).Select(f => new SelectOptionIdDto() {
                id = f.Id,
                text = f.Title
            }).ToListAsync();
            return Ok(pr);
        }
    }
}