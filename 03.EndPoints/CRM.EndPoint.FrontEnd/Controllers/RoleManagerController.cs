using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CRM.EndPoint.FrontEnd.Controllers
{
    [Authorize(Roles = "مدیر ارشد")]
    public class RoleManagerController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserContextDb udb;

        public RoleManagerController(RoleManager<IdentityRole> role ,UserContextDb udb)
        {
            roleManager = role;
            this.udb = udb;
        }
        public IActionResult Index()
        {
            var roles = roleManager.Roles.ToList();
            return View(roles);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(string name)
        {
            if (!roleManager.RoleExistsAsync(name).Result)
            {
                var result = roleManager.CreateAsync(new IdentityRole() {Name=name,NormalizedName="text" }).Result ;
            }
            return RedirectToAction("Index");
        }
        public IActionResult Delete(string id)
        {
            var role = roleManager.FindByIdAsync(id).Result;
            if (role != null)
            {
                var result = roleManager.DeleteAsync(role).Result;
            }
            return RedirectToAction("Index");
        }
    }
}
