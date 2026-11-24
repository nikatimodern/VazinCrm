using CRM.Core.Domain.BaseItems.Entities;
using CRM.Core.Domain.Company.Entities;
using CRM.Core.Domain.Products.Entities;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Security;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.BaseItems.Enums;
using CRM.Core.Domain.Cashes.Enums;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.BaseItem;
using CRM.Infrastructures.Sql;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize]
    public class BaseItemController : Controller {
        private readonly Service genService;
        private readonly FileSaver fileSaver;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly DocumentAccessPolicy access;

        public BaseItemController(Service GenService, FileSaver fileSaver, DateTimeServices dtService, CRMContextDb db, DocumentAccessPolicy access)
        {
            genService = GenService;
            this.fileSaver = fileSaver;
            this.dtService = dtService;
            this.db = db;
            this.access = access;
        }

        [LegacyCatalogRoles("BaseItem", "مدیر ارشد, مدیر فروش, مدیر تامین, مدیر مالی")]
        [CatalogPermission("BaseItem", "View")]
        public IActionResult Index()
        {
            return View();
        }

        [LegacyCatalogRoles("BaseCash", "مدیر ارشد, مدیر مالی, کارشناس مالی")]
        public async Task<IActionResult> Financial(CancellationToken ct)
        {
            if (!await access.HasViewAsync("BaseCash", ct)) return Forbid();
            var baseCashTypes = Enum.GetValues<BaseCashTypeEnum>().Select(x => new SelectOptionDto<int> {
                Value = (int)x,
                Text = x.ToString()
            }).ToList();

            var model = new FinancialSettingVModel {
                BaseCashTypes = baseCashTypes,
            };
            return View(model);
        }

        [CatalogPermission("Company", "View")]
        public async Task<IActionResult> getCompany()
        {
            var data = await db.Companies.Include(f => f.CompanyFiles).FirstOrDefaultAsync();
            return Ok(data);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [CatalogPermission("Company", "Edit")]
        public async Task<IActionResult> addCompany([FromBody] Company model)
        {
            if (model == null || !ModelState.IsValid) return BadRequest();
            var current = await db.Companies.Include(x => x.CompanyFiles).Take(2).ToListAsync();
            if (current.Count != 1 || current[0].Id != model.Id) return Conflict("اطلاعات شرکت تغییر کرده است؛ صفحه را تازه‌سازی کنید.");
            var files = (model.CompanyFiles ?? Array.Empty<CompanyFile>()).ToList();
            if (files.Count > 3 || files.Any(x => !ValidCompanyImage(x)) ||
                files.Select(x => x.Filename).Distinct(StringComparer.Ordinal).Count() != files.Count)
                return BadRequest("تصاویر لوگو و مهر معتبر نیستند.");

            var company = current[0];
            company.CompanyName = model.CompanyName;
            company.CompanyBrandName = model.CompanyBrandName;
            company.ShenaseMeli = model.ShenaseMeli;
            company.EghtesadiCode = model.EghtesadiCode;
            company.NumberRegister = model.NumberRegister;
            company.Tel = model.Tel;
            company.Mobile = model.Mobile;
            company.Address = model.Address;
            company.PostalCode = model.PostalCode;

            db.CompanyFile.RemoveRange(company.CompanyFiles ?? Array.Empty<CompanyFile>());
            company.CompanyFiles = files.Select(item => new CompanyFile {
                Id = Guid.NewGuid(), Filename = item.Filename,
                fileExten = item.fileExten.ToLowerInvariant(), filetype = item.filetype,
                file = item.file,
                FileUrl = fileSaver.SaveBaseItemImage(item.file, item.Filename, item.fileExten.ToLowerInvariant())
            }).ToList();
            await db.SaveChangesAsync(HttpContext.RequestAborted);
            return Ok();
        }

        // Restrict names and extensions before the legacy file saver constructs a physical path.
        private static bool ValidCompanyImage(CompanyFile item)
        {
            if (item == null || item.Filename is not ("Logo" or "Mohr" or "MohrR") ||
                item.file is not { Length: >= 12 and <= 20971520 }) return false;
            var bytes = item.file;
            return item.fileExten?.ToLowerInvariant() switch
            {
                "png" => bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71 &&
                         bytes[4] == 13 && bytes[5] == 10 && bytes[6] == 26 && bytes[7] == 10,
                "jpg" or "jpeg" => bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255,
                "gif" => bytes[0] == 71 && bytes[1] == 73 && bytes[2] == 70,
                "bmp" => bytes[0] == 66 && bytes[1] == 77,
                "webp" => bytes[0] == 82 && bytes[1] == 73 && bytes[2] == 70 && bytes[3] == 70 &&
                          bytes[8] == 87 && bytes[9] == 69 && bytes[10] == 66 && bytes[11] == 80,
                _ => false
            };
        }

        [CatalogPermission("BaseItem", "View")]
        public async Task<IActionResult> GetItemsOp(int typeCode)
        {
            var data = await db.BaseItems
                .Where(f => f.TypeCode == (BaseItemEnum)typeCode)
                .Select(g => new SelectOptionIdDto() {
                    id = g.Id,
                    text = g.Title
                }).ToListAsync();

            return Ok(data);
        }


        [CatalogPermission("Product", "View")]
        public async Task<IActionResult> getAllProduct(CancellationToken ct)
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
                var dataF = db.Products.AsNoTracking().Where(product => !product.IsDeleted);
                int recordsTotal = await dataF.CountAsync();

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
                    } else
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
                    dataF = dataF.Where(m =>
                        m.Title.Contains(searchValue)
                    );
                }

                // Group labels are catalog information, not part of the product-view grant.
                var canViewGroups = await access.HasAsync("ProductGroup", "View", ct);
                var recordsFiltered = await dataF.CountAsync(ct);
                var page = pageSize == -1 ? dataF : dataF.Skip(skip).Take(pageSize);
                var data = await page.Select(product => new
                {
                    product.Id,
                    product.Title,
                    GroupName = canViewGroups
                        ? db.ProductGroups.Where(group => group.Id == product.GroupId)
                            .Select(group => group.GroupName).FirstOrDefault()
                        : null
                }).ToListAsync(ct);
                var jsonData = new { draw, recordsFiltered, recordsTotal, data };
                return Ok(jsonData);
            }
            catch
            {
                return null;
            }
        }

        [CatalogPermission("BaseItem", "View")]
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
                int recordsTotal = await db.BaseItems.CountAsync(f => f.TypeCode == BaseItemEnum.طریقه_آشنایی);
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
                    } else
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
                    dataF = dataF.Where(m =>
                        m.Title.Contains(searchValue)
                    );
                }

                // DataTables sends -1 to request all matching rows.
                var recordsFiltered = await dataF.CountAsync();
                var data = pageSize == -1
                    ? await dataF.ToListAsync()
                    : await dataF.Skip(skip).Take(pageSize).ToListAsync();
                var jsonData = new { draw, recordsFiltered, recordsTotal, data };
                return Ok(jsonData);
            }
            catch
            {
                return null;
            }
        }

        [CatalogPermission("BaseItem", "View")]
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
                int recordsTotal = await db.BaseItems.CountAsync(f => f.TypeCode == BaseItemEnum.صنف);
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
                    } else
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
                    dataF = dataF.Where(m =>
                        m.Title.Contains(searchValue)
                    );
                }

                // DataTables sends -1 to request all matching rows.
                var recordsFiltered = await dataF.CountAsync();
                var data = pageSize == -1
                    ? await dataF.ToListAsync()
                    : await dataF.Skip(skip).Take(pageSize).ToListAsync();
                var jsonData = new { draw, recordsFiltered, recordsTotal, data };
                return Ok(jsonData);
            }
            catch
            {
                return null;
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [CatalogPermission("BaseItem", "Create")]
        public async Task<IActionResult> AddItem(BaseItem model)
        {
            if (model == null || !ModelState.IsValid || string.IsNullOrWhiteSpace(model.Title) || !Enum.IsDefined(model.TypeCode)) return BadRequest();
            model.Id = Guid.NewGuid();
            model.Title = model.Title.Trim();
            model.TypeTxt = model.TypeCode.ToString().Replace("_", " ");
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


        [HttpPost, ValidateAntiForgeryToken]
        [CatalogPermission("Product", "Create")]
        public async Task<IActionResult> AddProduct([FromForm] string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return BadRequest();
            db.Products.Add(new Product { Title = title.Trim() });
            await db.SaveChangesAsync();

            return Ok();
        }
    }
}