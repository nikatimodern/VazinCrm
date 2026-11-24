using CRM.EndPoint.FrontEnd.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models;
using CRM.Infrastructures.Sql;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize]
    public class ProductController : Controller {
        private readonly CRMContextDb db;
        private readonly DocumentAccessPolicy access;

        public ProductController(CRMContextDb db, DocumentAccessPolicy access)
        {
            this.db = db;
            this.access = access;
        }

        [HttpGet]
        [CatalogPermission("Product", "View")]
        public async Task<IActionResult> GetAllProduct(Guid? groupId, CancellationToken ct)
        {
            var products = db.Products.AsNoTracking().Where(f => !f.IsDeleted);
            if (groupId.HasValue)
            {
                if (!await access.HasAsync("ProductGroup", "View", ct)) return Forbid();
                products = products.Where(f => f.GroupId == groupId);
            }
            var pr = await products.OrderBy(f => f.Title).Select(f => new SelectOptionIdDto() {
                id = f.Id,
                text = f.Title
            }).ToListAsync(ct);
            return Ok(pr);
        }

        [HttpGet]
        [CatalogPermission("ProductGroup", "View")]
        public async Task<IActionResult> GetAllProductGroups(CancellationToken ct)
        {
            var groups = await db.ProductGroups.AsNoTracking().OrderBy(group => group.GroupName)
                .Select(group => new SelectOptionIdDto { id = group.Id, text = group.GroupName }).ToListAsync(ct);
            return Ok(groups);
        }
    }
}