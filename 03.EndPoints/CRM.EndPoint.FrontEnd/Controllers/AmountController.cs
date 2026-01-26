using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Products.Entities;
using CRM.EndPoint.FrontEnd.Services;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.LogSystems.Entities;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize(Roles = " کارشناس فروش ,مدیر فروش , مدیر تامین ,  مدیر ارشد , کارشناس تامین")]

    //[Authorize]
    public class AmountController : Controller {
        private readonly UserManager<AppUser> userManager;
        private readonly Service genService;
        private readonly FileSaver fileSaver;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly UserContextDb udb;
        private readonly LogSystemService _logSystemService;

        public AmountController(UserManager<AppUser> userManager, Service GenService, FileSaver fileSaver,
            DateTimeServices dtService, CRMContextDb db, UserContextDb udb,LogSystemService logSystemService) {
            this.userManager = userManager;
            genService = GenService;
            this.fileSaver = fileSaver;
            this.dtService = dtService;
            this.db = db;
            this.udb = udb;
            _logSystemService = logSystemService;
        }


        public async Task<IActionResult> index() {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> ListProduct(string searchValue, string from = "amountIndex") {
            try {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var roles = await userManager.GetRolesAsync(user);

                ViewBag.hasAccess = false;
                ViewBag.from = from;
                if (roles.Contains("مدیر ارشد") || roles.Contains("مدیر فروش")  || roles.Contains("مدیر تامین")  || roles.Contains("کارشناس تامین") )
                {
                    ViewBag.hasAccess = true;
                }

                var pr = await (from mo in db.Products
                    where EF.Functions.Like(mo.Title, "%" + searchValue + "%")
                    select mo).Include(f => f.ProductAmounts).ToListAsync();

                foreach (var item in pr) {
                    foreach (var pa in item.ProductAmounts) {
                        if (pa.ModifyDate == null)
                            pa.ElapsTime = dtService.ElapsTime(pa.CreateDate);
                        else
                            pa.ElapsTime = dtService.ElapsTime(pa.ModifyDate.Value);
                    }
                }


                //var products = await db.Products.Include(f=>f.ProductAmounts).ToListAsync();
                return PartialView(pr);
            }
            catch {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddAmount([FromBody] ProductAmount model) {
            var user = userManager.GetUserAsync(HttpContext.User).Result;

            if (model.Id != Guid.Empty) {

                var oldModel = await db.ProductAmounts.AsNoTracking().Where(f => f.Id == model.Id).FirstOrDefaultAsync();

                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.ModifyDate = DateTime.Now;
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;

                db.ProductAmounts.Update(model);

                db.Entry(model).Property(f => f.CreateDate).IsModified = false;
                db.Entry(model).Property(f => f.CreateDateTxt).IsModified = false;
                db.Entry(model).Property(f => f.UserCreator).IsModified = false;
                db.Entry(model).Property(f => f.UserCreatorID).IsModified = false;

                await _logSystemService.LogChange(oldModel, model, LogActionEnum.ویرایش, user);
            }
            else {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;

                db.ProductAmounts.Add(model);
                await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> DelAmount(Guid id) {
            var user = userManager.GetUserAsync(HttpContext.User).Result;
            var pro = await db.ProductAmounts.Where(f => f.Id == id).FirstOrDefaultAsync();
            await _logSystemService.LogChange(pro, null, LogActionEnum.حذف, user);

            pro.IsDeleted = true;
            pro.UserModify = user.FullName;
            pro.UserModifyID = user.Id;
            pro.ModifyDate = DateTime.Now;
            pro.ModifyDateTxt = dtService.GetDateTimePersian();

            await db.SaveChangesAsync();

            return Ok();
        }
    }
}