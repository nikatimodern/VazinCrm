using CRM.Core.Domain.BaseItems.Entities;
using CRM.Core.Domain.Company.Entities;
using CRM.Core.Domain.Products.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Services;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.BaseItems.Enums;

namespace CRM.EndPoint.FrontEnd.Controllers
{
    [Authorize]

    public class BaseItemController : Controller
    {
        private readonly Service genService;
        private readonly FileSaver fileSaver;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        public BaseItemController(Service GenService, FileSaver fileSaver, DateTimeServices dtService, CRMContextDb db)
        {
            genService = GenService;
            this.fileSaver = fileSaver;
            this.dtService = dtService;
            this.db = db;
        }

        [Authorize(Roles = "مدیر ارشد, مدیر فروش, مدیر تامین, مدیر مالی")]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
        public IActionResult Financial()
        {
            return View();
        }

        public async Task<IActionResult> getCompany()
        {
            var data= await db.Companies.Include(f => f.CompanyFiles).FirstOrDefaultAsync();
            return Ok(data);
        }
        public async Task<IActionResult> addCompany(Company model)
        {
            db.CompanyFile.RemoveRange(db.CompanyFile.ToList());
            foreach(var item in model.CompanyFiles ?? Enumerable.Empty<CompanyFile>())
            {
                
                item.FileUrl = fileSaver.SaveBaseItemImage(item.file, item.Filename, item.fileExten);
            }
            db.Companies.Update(model);
            await db.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> GetItemsOp(int typeCode)
        {
            var data = await db.BaseItems
                .Where(f => f.TypeCode == (BaseItemEnum)typeCode)
                .Select(g => new SelectOptionGuidDto()
                {
                    id = g.Id,
                    text = g.Title
                }).ToListAsync();

            return Ok(data);
        }


        public async Task<IActionResult> getAllProduct()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                var dataF = db.Products.AsQueryable();

                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    if (sortColumnDirection == "asc")
                    {
                        switch (sortColumn)
                        {
                            case "Title":
                                dataF = dataF.OrderBy(f => f.Title);
                                break;
                            default:
                                dataF = dataF.OrderBy(f => f.Title);
                                break;
                        }
                    }
                    else
                    {
                        switch (sortColumn)
                        {
                            case "Title":
                                dataF = dataF.OrderByDescending(f => f.Title);
                                break;
                            default:
                                dataF = dataF.OrderByDescending(f => f.Title);
                                break;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(searchValue))
                {
                    dataF = dataF.Where(
                        m =>
                        m.Title.Contains(searchValue)
                    );
                }
                recordsTotal = dataF.Count();
                var data = await dataF.Skip(skip).Take(pageSize).ToListAsync();
                var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
                return Ok(jsonData);
            }
            catch 
            {
                return null;
            }
        }

        public async Task<IActionResult> GetAllFamillyWay()
        {

            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                var dataF = db.BaseItems.Where(f => f.TypeCode == BaseItemEnum.طریقه_آشنایی).AsQueryable();

                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    if (sortColumnDirection == "asc")
                    {
                        switch (sortColumn)
                        {
                            case "Title":
                                dataF = dataF.OrderBy(f => f.Title);
                                break;
                            default:
                                dataF = dataF.OrderBy(f => f.Title);
                                break;
                        }
                    }
                    else
                    {
                        switch (sortColumn)
                        {
                            case "Title":
                                dataF = dataF.OrderByDescending(f => f.Title);
                                break;
                            default:
                                dataF = dataF.OrderByDescending(f => f.Title);
                                break;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(searchValue))
                {
                    dataF = dataF.Where(
                        m =>
                        m.Title.Contains(searchValue)
                    );
                }
                recordsTotal = dataF.Count();
                var data = await dataF.Skip(skip).Take(pageSize).ToListAsync();
                var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
                return Ok(jsonData);
            }
            catch 
            {
                return null;
            }




        }

        public async Task<IActionResult> GetAllSenf()
        {

            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                var dataF = db.BaseItems.Where(f => f.TypeCode == BaseItemEnum.صنف).AsQueryable();

                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    if (sortColumnDirection == "asc")
                    {
                        switch (sortColumn)
                        {
                            case "Title":
                                dataF = dataF.OrderBy(f => f.Title);
                                break;
                            default:
                                dataF = dataF.OrderBy(f => f.Title);
                                break;
                        }
                    }
                    else
                    {
                        switch (sortColumn)
                        {
                            case "Title":
                                dataF = dataF.OrderByDescending(f => f.Title);
                                break;
                            default:
                                dataF = dataF.OrderByDescending(f => f.Title);
                                break;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(searchValue))
                {
                    dataF = dataF.Where(
                        m =>
                        m.Title.Contains(searchValue)
                    );
                }
                recordsTotal = dataF.Count();
                var data = await dataF.Skip(skip).Take(pageSize).ToListAsync();
                var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
                return Ok(jsonData);
            }
            catch
            {
                return null;
            }

        }

        public async Task<IActionResult> AddItem(BaseItem model)
        {
            using var transaction = db.Database.BeginTransaction();
            try
            {

                db.Add(model);
                await db.SaveChangesAsync();
                transaction.Commit();
                return Ok();
            }
            catch
            {
                transaction.Rollback();
                return BadRequest();
            }

        }


        [HttpPost]
        public async Task<IActionResult> AddProduct(Product model)
        {
            db.Products.Add(model);
            await db.SaveChangesAsync();

            return Ok();
        }

    }
}
