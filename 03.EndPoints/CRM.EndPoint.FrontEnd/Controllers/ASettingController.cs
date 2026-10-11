using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.EndPoint.FrontEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Setting;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.Infrastructures.Sql;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize]
    public class ASettingController : Controller {
        private readonly UserManager<AppUser> userManager;
        private readonly Service genService;
        private readonly FileSaver fileSaver;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly LogSystemService _logSystemService;

        public ASettingController(UserManager<AppUser> userManager, Service GenService, FileSaver fileSaver,
            DateTimeServices dtService, CRMContextDb db, LogSystemService logSystemService)
        {
            this.userManager = userManager;
            genService = GenService;
            this.fileSaver = fileSaver;
            this.dtService = dtService;
            this.db = db;
            _logSystemService = logSystemService;
        }

        [Authorize(Roles = "مدیر ارشد")]
        public IActionResult Index()
        {
            return View();
        }


        [Authorize(Roles = "مدیر ارشد")]
        public async Task<IActionResult> RecycleBin()
        {
            return View();
        }

        [Authorize(Roles = "مدیر ارشد")]
        [HttpPost]
        public async Task<IActionResult> GetAllDeleted()
        {
            try
            {
                var modelF = JsonConvert.DeserializeObject<FilterVModel>(Request.Form["modelF"].FirstOrDefault());
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                var model = new List<AllDeletedVModel>();

                /// customer
                var Qcus = db.Customers.IgnoreQueryFilters().AsNoTracking().Where(f => f.IsDeleted == true).AsQueryable();
                recordsTotal += Qcus.Count();


                var cus = new List<Customer>();
                if (pageSize == -1)
                    cus = await Qcus.OrderByDescending(f => f.ModifyDate).Skip(skip).ToListAsync();
                else
                    cus = await Qcus.OrderByDescending(f => f.ModifyDate).Skip(skip).Take(pageSize).ToListAsync();


                foreach (var item in cus)
                {
                    model.Add(new AllDeletedVModel() {
                        EntityId = item.Id,
                        EntityType = EntityTypeEnum.Customer,
                        EntityTypeTxt = "مشتری",
                        title = item.CompanyName + " " + item.AgentName,
                        ModifyDate = item.ModifyDate.Value,
                        ModifyDateTxt = string.Join(" | ", item.ModifyDateTxt.Substring(0, 16).Split(' ').Reverse()),
                        CodeNumber = item.CustomerCode.ToString(),
                        ModifyUser = item.UserModify
                    });
                }

                ////endCustomer


                IQueryable<AllDeletedVModel> dataF;
                dataF = model.AsQueryable();


                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    if (sortColumnDirection == "asc")
                    {
                        switch (sortColumn)
                        {
                            case "dateSh":
                                dataF = dataF.OrderBy(f => f.ModifyDateTxt);
                                break;

                            default:
                                dataF = dataF.OrderBy(f => f.ModifyDateTxt);
                                break;
                        }
                    } else
                    {
                        switch (sortColumn)
                        {
                            case "dateSh":
                                dataF = dataF.OrderByDescending(f => f.ModifyDateTxt);
                                break;

                            default:
                                dataF = dataF.OrderByDescending(f => f.ModifyDateTxt);
                                break;
                        }
                    }
                }

                if (modelF.filt != null)
                {
                    dataF = dataF.ApplyFilter(modelF, null, f => f.ModifyDate, null);
                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    dataF = dataF.Where(m =>
                            m.CodeNumber.ToString() == searchValue
                        //m.Contains(searchValue) ||
                        //m.CustomerCode.ToString() == searchValue ||
                        //m.BankAccountTxt.Contains(searchValue) ||
                        //m.credit.ToString() == (searchValue)
                    ).AsQueryable();
                }


                //recordsTotal = dataF.Count();
                List<AllDeletedVModel> data = null;
                //if (pageSize == -1)
                //    data =  dataF.Skip(skip).ToList();
                //else
                //    data =  dataF.Skip(skip).Take(pageSize).ToList();

                data = dataF.ToList();

                var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
                return Ok(jsonData);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [Authorize(Roles = "مدیر ارشد")]
        public async Task<ActionResult> GetRecycleItems(int skip = 0, int take = 10, string search = "", int recycleType = -1)
        {
            try
            {
                var model = new List<AllDeletedVModel>();

                async Task AddEntities<T>(IQueryable<T> query, Func<T, AllDeletedVModel> map)
                {
                    var list = await query.ToListAsync();
                    model.AddRange(list.Select(map));
                }

                if (recycleType == -1 || recycleType == (int)EntityTypeEnum.Customer)
                {
                    var query = db.Customers.IgnoreQueryFilters().AsNoTracking().Where(f => f.IsDeleted);
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        query = query.Where(f =>
                            f.CompanyName.Contains(search) || f.AgentName.Contains(search) ||
                            f.CustomerCode.ToString() == search || f.UserModify.Contains(search));
                    }

                    await AddEntities(query.Select(f => new {
                            f.Id,
                            f.CompanyName,
                            f.AgentName,
                            f.ModifyDate,
                            f.ModifyDateTxt,
                            f.CustomerCode,
                            f.UserModify
                        }),
                        item => new AllDeletedVModel {
                            EntityId = item.Id,
                            EntityType = EntityTypeEnum.Customer,
                            EntityTypeTxt = "مشتری",
                            title = $"{item.CompanyName} {item.AgentName}",
                            ModifyDate = item.ModifyDate ?? DateTime.MinValue,
                            ModifyDateTxt = FormatDateTime(item.ModifyDateTxt),
                            CodeNumber = item.CustomerCode.ToString(),
                            ModifyUser = item.UserModify ?? "-"
                        });
                }

                if (recycleType == -1 || recycleType == (int)EntityTypeEnum.PreFactor)
                {
                    var query = db.PreFactors.IgnoreQueryFilters().AsNoTracking().Where(f => f.IsDeleted);
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        query = query.Where(f =>
                            f.Customer.Contains(search) ||
                            f.AgentName.Contains(search) ||
                            f.SerialNumber.ToString() == search ||
                            (f.CustomerEntity != null && f.CustomerEntity.CustomerCode.ToString() == search)
                            || f.UserModify.Contains(search));
                    }


                    await AddEntities(query.Select(f => new {
                            f.Id,
                            f.Customer,
                            f.AgentName,
                            f.ModifyDate,
                            f.ModifyDateTxt,
                            f.SerialNumber,
                            f.UserModify,
                            CustomerCode = f.CustomerEntity != null ? f.CustomerEntity.CustomerCode : (int?)null
                        }),
                        item => new AllDeletedVModel {
                            EntityId = item.Id,
                            EntityType = EntityTypeEnum.PreFactor,
                            EntityTypeTxt = "پیش فاکتور",
                            title = $"{item.Customer} {item.AgentName} ({item.CustomerCode?.ToString() ?? "-"})",
                            ModifyDate = item.ModifyDate ?? DateTime.MinValue,
                            ModifyDateTxt = FormatDateTime(item.ModifyDateTxt),
                            CodeNumber = item.SerialNumber.ToString(),
                            ModifyUser = item.UserModify ?? "-"
                        });
                }

                if (recycleType == -1 || recycleType == (int)EntityTypeEnum.Factor)
                {
                    var query = db.Factors.IgnoreQueryFilters().AsNoTracking().Where(f => f.IsDeleted);
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        query = query.Where(f =>
                            f.Customer.Contains(search) || f.AgentName.Contains(search) ||
                            f.SerialNumber.ToString() == search || f.CustomerCode.ToString() == search
                            || f.UserModify.Contains(search));
                    }

                    await AddEntities(query.Select(f => new {
                            f.Id,
                            f.Customer,
                            f.AgentName,
                            f.ModifyDate,
                            f.ModifyDateTxt,
                            f.SerialNumber,
                            f.UserModify,
                            f.CustomerCode
                        }),
                        item => new AllDeletedVModel {
                            EntityId = item.Id,
                            EntityType = EntityTypeEnum.Factor,
                            EntityTypeTxt = "فاکتور",
                            title = $"{item.Customer} {item.AgentName} ({item.CustomerCode?.ToString() ?? "-"})",
                            ModifyDate = item.ModifyDate ?? DateTime.MinValue,
                            ModifyDateTxt = FormatDateTime(item.ModifyDateTxt),
                            CodeNumber = item.SerialNumber.ToString(),
                            ModifyUser = item.UserModify ?? "-"
                        });
                }

                if (recycleType == -1 || recycleType == (int)EntityTypeEnum.OrderHead)
                {
                    var query = db.OrderHeads.IgnoreQueryFilters().AsNoTracking().Where(f => f.IsDeleted);
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        query = query.Where(f =>
                            f.CustomerTxt.Contains(search)
                            || f.OrderNumber.ToString() == search
                            || f.CustomerCode.ToString() == search
                            || f.UserModify.Contains(search));
                    }

                    await AddEntities(query.Select(f => new {
                            f.Id,
                            f.CustomerTxt,
                            f.ModifyDate,
                            f.ModifyDateTxt,
                            f.OrderNumber,
                            f.UserModify,
                            f.CustomerCode
                        }),
                        item => new AllDeletedVModel {
                            EntityId = item.Id,
                            EntityType = EntityTypeEnum.OrderHead,
                            EntityTypeTxt = "حواله تولید",
                            title = $"{item.CustomerTxt} ({item.CustomerCode})",
                            ModifyDate = item.ModifyDate ?? DateTime.MinValue,
                            ModifyDateTxt = FormatDateTime(item.ModifyDateTxt),
                            CodeNumber = item.OrderNumber.ToString(),
                            ModifyUser = item.UserModify ?? "-"
                        });
                }

                if (recycleType == -1 || recycleType == (int)EntityTypeEnum.SupplyFactor)
                {
                    var query = db.SupplyFactors.IgnoreQueryFilters().AsNoTracking().Where(f => f.IsDeleted);
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        query = query.Where(f =>
                            f.SerialNumber == search
                            || f.SupplierTxt.Contains(search)
                            || f.UserModify.Contains(search)
                        );
                    }

                    await AddEntities(query.Select(f => new {
                            f.Id,
                            FactorSerial = f.SerialNumber,
                            f.ModifyDate,
                            f.ModifyDateTxt,
                            f.SupplierTxt,
                            f.UserModify,
                        }),
                        item => new AllDeletedVModel {
                            EntityId = item.Id,
                            EntityType = EntityTypeEnum.SupplyFactor,
                            EntityTypeTxt = "فاکتور تامین",
                            title = item.SupplierTxt,
                            ModifyDate = item.ModifyDate ?? DateTime.MinValue,
                            ModifyDateTxt = FormatDateTime(item.ModifyDateTxt),
                            CodeNumber = item.FactorSerial.ToString(),
                            ModifyUser = item.UserModify ?? "-"
                        });
                }

                if (recycleType == -1 || recycleType == (int)EntityTypeEnum.Payment)
                {
                    var query = db.Payments.IgnoreQueryFilters().AsNoTracking().Where(f => f.IsDeleted);
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        query = query.Where(f =>
                            f.PaymentNumber == TextService.convertToInt(search)
                            || f.ToAccountTxt.Contains(search)
                            || f.UserModify.Contains(search)
                        );
                    }

                    await AddEntities(query.Select(f => new {
                            f.Id,
                            f.PaymentNumber,
                            f.ModifyDate,
                            f.ModifyDateTxt,
                            f.ToAccountTxt,
                            f.UserModify,
                        }),
                        item => new AllDeletedVModel {
                            EntityId = item.Id,
                            EntityType = EntityTypeEnum.Payment,
                            EntityTypeTxt = "پرداختی",
                            title = $"پرداختی به {item.ToAccountTxt}",
                            ModifyDate = item.ModifyDate ?? DateTime.MinValue,
                            ModifyDateTxt = FormatDateTime(item.ModifyDateTxt),
                            CodeNumber = item.PaymentNumber.ToString(),
                            ModifyUser = item.UserModify ?? "-"
                        });
                }

                if (recycleType == -1 || recycleType == (int)EntityTypeEnum.RequestAmount)
                {
                    var query = db.RequestAmounts.IgnoreQueryFilters().AsNoTracking().Where(f => f.IsDeleted);
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var searchNumber = TextService.convertToInt(search);
                        query = query.Where(f =>
                            f.RequestNumber == searchNumber
                            || f.Amount == searchNumber
                            || f.UserModify.Contains(search)
                        );
                    }

                    await AddEntities(query.Select(f => new {
                            f.Id,
                            f.RequestNumber,
                            f.ModifyDate,
                            f.ModifyDateTxt,
                            f.Amount,
                            f.UserModify,
                        }),
                        item => new AllDeletedVModel {
                            EntityId = item.Id,
                            EntityType = EntityTypeEnum.RequestAmount,
                            EntityTypeTxt = "درخواست وجه",
                            title = $"وجه {item.Amount:N0}",
                            ModifyDate = item.ModifyDate ?? DateTime.MinValue,
                            ModifyDateTxt = FormatDateTime(item.ModifyDateTxt),
                            CodeNumber = item.RequestNumber.ToString(),
                            ModifyUser = item.UserModify ?? "-"
                        });
                }

                int total = model.Count;
                var pagedData = model.OrderByDescending(f => f.ModifyDate).Skip(skip).Take(take).ToList();

                return Json(new { total, data = pagedData });
            }
            catch (Exception ex)
            {
                return Json(new { total = 0, data = new List<AllDeletedVModel>(), error = ex.Message });
            }
        }

        private string FormatDateTime(string datetime)
        {
            return (datetime != null && (datetime).Length >= 16)
                ? string.Join(" | ", (datetime).Substring(0, 16).Split(' ').Reverse())
                : (datetime ?? "-");
        }


        [Authorize(Roles = "مدیر ارشد")]
        [HttpGet]
        public async Task<IActionResult> RestoreEntity(Guid eId, EntityTypeEnum eType)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var dataNow = DateTime.Now;
            var dataNowPersian = dtService.GetDateTimePersian();

            void SetMainFields(dynamic entity)
            {
                entity.IsDeleted = false;
                entity.UserModify = user.FullName;
                entity.UserModifyID = user.Id;
                entity.ModifyDate = dataNow;
                entity.ModifyDateTxt = dataNowPersian;
            }

            switch (eType)
            {
                case EntityTypeEnum.Customer:
                {
                    var customer = await db.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == eId);
                    await _logSystemService.LogChange(customer, null, LogActionEnum.بازیابی, user);
                    SetMainFields(customer);

                    await db.SaveChangesAsync();
                    break;
                }
                case EntityTypeEnum.PreFactor:
                {
                    var preF = await db.PreFactors.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == eId);
                    await _logSystemService.LogChange(preF, null, LogActionEnum.بازیابی, user);
                    SetMainFields(preF);
                    preF.CommentText = null;
                    preF.Pftype = preF.PrePftype ?? 0;
                    preF.PftypeTxt = Service.GetTypeTxtPfactor(preF.Pftype);

                    if (preF.CustomerId != null)
                    {
                        var cus = await db.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == preF.CustomerId);
                        if (cus.IsDeleted)
                        {
                            await _logSystemService.LogChange(cus, null, LogActionEnum.بازیابی, user);
                            cus.IsDeleted = false;
                            cus.ModifyDate = dataNow;
                            cus.ModifyDateTxt = dataNowPersian;
                        }
                    }

                    var accS = await db.Accountancies.Where(f =>
                        f.pfid == preF.Id
                        && f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                        && (f.AccType == AcEnum.واریزی || f.AccType == AcEnum.برگشتی)
                    ).ToListAsync();

                    foreach (var acc in accS)
                        acc.Wallet = false;

                    await db.SaveChangesAsync();
                    break;
                }
                case EntityTypeEnum.Factor:
                {
                    var fac = await db.Factors.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == eId);
                    await _logSystemService.LogChange(fac, null, LogActionEnum.بازیابی, user);
                    SetMainFields(fac);

                    if (fac.CustomerId != null)
                    {
                        var cusf = await db.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == fac.CustomerId);
                        if (cusf.IsDeleted)
                        {
                            await _logSystemService.LogChange(cusf, null, LogActionEnum.بازیابی, user);
                            cusf.IsDeleted = false;
                            cusf.ModifyDate = dataNow;
                            cusf.ModifyDateTxt = dataNowPersian;
                        }
                    }

                    if (fac.ParentReturnedId != null)
                    {
                        var returnedF = await db.Factors.FirstOrDefaultAsync(f => f.Id == fac.ParentReturnedId);
                        returnedF?.Returned = true;
                    }
                    /*
                    var ac = await db.Accountancies.IgnoreQueryFilters().Where(f => f.fid == EId).FirstOrDefaultAsync();
                    if (ac != null)
                        ac.IsDeleted = false;
                    */

                    await db.SaveChangesAsync();
                    break;
                }
                case EntityTypeEnum.OrderHead:
                {
                    var orderH = await db.OrderHeads.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == eId);
                    await _logSystemService.LogChange(orderH, null, LogActionEnum.بازیابی, user);
                    SetMainFields(orderH);

                    await db.SaveChangesAsync();
                    break;
                }

                case EntityTypeEnum.SupplyFactor:
                {
                    var supplyFactor = await db.SupplyFactors.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == eId);
                    await _logSystemService.LogChange(supplyFactor, null, LogActionEnum.بازیابی, user);
                    SetMainFields(supplyFactor);

                    await db.SaveChangesAsync();
                    break;
                }
                case EntityTypeEnum.Payment:
                {
                    var payment = await db.Payments.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == eId);
                    await _logSystemService.LogChange(payment, null, LogActionEnum.بازیابی, user);
                    SetMainFields(payment);

                    await db.SaveChangesAsync();
                    break;
                }
                case EntityTypeEnum.RequestAmount:
                {
                    var requestAmount = await db.RequestAmounts.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == eId);
                    await _logSystemService.LogChange(requestAmount, null, LogActionEnum.بازیابی, user);
                    SetMainFields(requestAmount);

                    await db.SaveChangesAsync();
                    break;
                }
            }

            return Ok();
        }


        [Authorize(Roles = "مدیر ارشد")]
        public async Task<IActionResult> upSetting(ASetting model)
        {
            try
            {
                db.ASettings.Update(model);
                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        public async Task<IActionResult> GetSetting()
        {
            try
            {
                if (db.ASettings.Count() != 0)
                {
                    var data = await db.ASettings.FirstOrDefaultAsync();
                    return Ok(data);
                }

                return Ok(null);
            }
            catch
            {
                return BadRequest();
            }
        }


        [Authorize(Roles = "مدیر ارشد")]
        public IActionResult ExternalSoftware()
        {
            return View();
        }

        [Authorize(Roles = "مدیر ارشد")]
        [HttpPost]
        public async Task<IActionResult> AddExternalSoftware([FromBody] ExternalSoftware model)
        {
            try
            {
                db.ExternalSoftwares.Add(model);
                await db.SaveChangesAsync();

                return Ok();
            }
            catch
            {
                return BadRequest();
            }
        }

        public async Task<IActionResult> GetExternalSoftwares()
        {
            try
            {
                var data = await db.ExternalSoftwares.AsNoTracking().ToListAsync();

                return Ok(data);
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}