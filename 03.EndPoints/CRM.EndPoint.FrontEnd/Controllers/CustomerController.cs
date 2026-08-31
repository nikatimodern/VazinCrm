using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.Factor;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.IServices.PushNotification;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Customer;
using CRM.EndPoint.FrontEnd.Models.PreFactor;
using CRM.EndPoint.FrontEnd.Models.PushNotification;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.LogSystem;


namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize]
    public class CustomerController : Controller {
        private readonly SepidarService sepidarService;
        private readonly UserManager<AppUser> userManager;
        private readonly Service genService;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly UserContextDb udb;
        private readonly IHubContext<TaskHub> hub;
        private readonly IViewRenderService vrs;
        private readonly LogSystemService _logSystemService;
        private readonly IPushQueue pushQueue;
        private readonly PreFactorService preFactorService;

        public CustomerController(SepidarService sepidarService, UserManager<AppUser> userManager, Service GenService,
            DateTimeServices dtService, CRMContextDb db, UserContextDb udb,
            IHubContext<TaskHub> hub, IViewRenderService vrs, LogSystemService logSystemService, IPushQueue pushQueue,
            PreFactorService preFactorService)
        {
            this.sepidarService = sepidarService;
            this.userManager = userManager;
            genService = GenService;
            this.dtService = dtService;
            this.db = db;
            this.udb = udb;
            this.hub = hub;
            this.vrs = vrs;
            _logSystemService = logSystemService;
            this.pushQueue = pushQueue;
            this.preFactorService = preFactorService;
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public async Task<IActionResult> Index()
        {
            //           await NormalizeAllCustomers();

            ViewBag.SubTitle = " فرم ثبت مشتری";
            ViewBag.Head = "Customer";

            var user = await userManager.GetUserAsync(User);
            return View(user);
        }


        [Authorize(Policy = "NotCustomerOnly")]
        [HttpPost]
        public async Task<IActionResult> RemoveUserCustomer([FromBody] UserCustomer model)
        {
            var user = userManager.GetUserAsync(HttpContext.User).Result;
            var cu = await db.UserCustomers.Where(f => f.UserId == model.UserId && f.CustomerId == model.CustomerId).FirstOrDefaultAsync();
            //var cus = await db.Customers.AsNoTracking().Where(f => f.Id == model.CustomerId).Include(f => f.UserCustomers).FirstOrDefaultAsync();
            //await _logSystemService.LogChange(cu, null, logActionEnum.حذف ,user.FullName);

            db.UserCustomers.Remove(cu);
            await db.SaveChangesAsync();

            return Ok();
        }

        [Authorize(Policy = "NotCustomerOnly")]
        [HttpPost]
        public async Task<IActionResult> AddSelsToCustomers([FromBody] SelsCustomerVModel model)
        {
            var user = userManager.GetUserAsync(HttpContext.User).Result;
            //var cus = await db.Customers.AsNoTracking().Where(f => f.Id == model.customerId).Include(f => f.UserCustomers).FirstOrDefaultAsync();
            //await _logSystemService.LogChange(cus, logActionEnum.ویرایش ,user.FullName);

            foreach (var item in model.selExperts)
            {
                db.UserCustomers.Add(new UserCustomer {
                    CustomerId = model.customerId,
                    UserId = item
                });
            }

            await db.SaveChangesAsync();
            return Ok();
        }


        [Authorize(Policy = "NotCustomerOnly")]
        [HttpGet]
        public async Task<IActionResult> GetCustomerOp(string search)
        {
            if (search == null)
                return Ok(null);
            else
            {
                var data = await (from cus in db.Customers
                        where EF.Functions.Like(cus.AgentName, "%" + search + "%") ||
                              EF.Functions.Like(cus.CompanyName, "%" + search + "%") ||
                              EF.Functions.Like(cus.CustomerCode.ToString(), "%" + search + "%")
                        select cus)
                    .Select(g => new SelectOptionIdDto() {
                        id = g.Id,
                        text = g.CompanyName + " " + g.AgentName + " کد مشتری: " + g.CustomerCode
                    }).Take(20).ToListAsync();

                return Ok(data);
            }
        }

        [Authorize(Policy = "NotCustomerOnly")]
        [HttpGet]
        public async Task<IActionResult> GetSuppliers()
        {
            var data = await db.Customers
                .Where(f => f.RoleType == RoleTypeEnum.تامین_کننده || f.RoleType == RoleTypeEnum.تامین_کننده_مشتری)
                .Select(f => new { f.Id, f.CompanyName, f.AgentName, f.CustomerCode })
                .ToListAsync();

            return Ok(data);
        }

        [Authorize(Policy = "NotCustomerOnly")]
        [HttpGet]
        public async Task<IActionResult> GetCustomerRel(Guid Id)
        {
            var cus = await db.Customers.Where(f => f.Id == Id).FirstOrDefaultAsync();
            IQueryable<Customer> customers;
            if (cus.Juridical == 1)
            {
                customers = (from _customers in db.Customers
                    where
                        ((_customers.ShenaseMeli == cus.ShenaseMeli && cus.ShenaseMeli != null)
                         || (_customers.EghtesadiCode == cus.EghtesadiCode && cus.EghtesadiCode != null)
                         || (_customers.NumberRegister == cus.NumberRegister && cus.NumberRegister != null)
                        )
                        && _customers.Id != cus.Id
                    select _customers).AsQueryable();
            } else
            {
                customers = (from _customers in db.Customers
                    where
                        EF.Functions.Like(_customers.AgentName, "%" + cus.AgentName + "%") ||
                        //EF.Functions.Like(_customers.CompanyName, "%" + cus.CompanyName + "%") ||
                        (_customers.Mobile == cus.Mobile && cus.Mobile != null)
                        && (_customers.Id != cus.Id)
                    select _customers).AsQueryable();
            }

            var d = await customers.ToListAsync();
            if (customers.Any())
            {
                return PartialView("DuplicatePartialDetail", await customers.ToListAsync());
            } else
            {
                return Ok(null);
            }
        }

        private async Task<bool> CanAccessCustomerProfile(Guid customerId)
        {
            if (User.IsAdmin() || User.IsSalesAdmin())
                return true;

            if (User.IsCustomer())
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                return await db.Customers.AnyAsync(x => x.Id == customerId && x.Mobile == user.PhoneNumber);
            }

            var userId = Guid.Parse(User.GetUserId());
            return await db.UserCustomers.AnyAsync(x => x.UserId == userId && x.CustomerId == customerId);
        }


        [HttpGet]
        public async Task<IActionResult> CustomerProfile(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest();

            var query = db.Customers
                .AsNoTracking()
                .Include(x => x.UserCustomers)
                .AsQueryable();

            if (User.IsAdmin())
            {
                query = query.IgnoreQueryFilters();
            }

            var customer = await query.FirstOrDefaultAsync(x => x.Id == id);


            if (customer == null)
                return NotFound();

            if (!await CanAccessCustomerProfile(id))
                return Forbid();

            var model = new CustomerPanelVModel {
                CustomerId = customer.Id,
                CustomerName =
                    $"پنل مشتری {customer.CustomerCode} {customer.CompanyName} " +
                    $"({(customer.Sex == 1 ? "جناب آقای" : "سرکار خانم")} {customer.AgentName})",
                SelExpertRel = await GetUsersRel(customer.UserCustomers)
            };

            return View(model);
        }

        [Authorize(Roles = "مدیر ارشد ,مدیر فروش")]
        [HttpGet]
        public ActionResult CustomerDetail(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest();

            ViewBag.CustomerId = id;
            return View();
        }


        [Authorize(Policy = "NotCustomerOnly")]
        [HttpGet]
        public async Task<IActionResult> AbortCustomer(Guid id)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var cu = await db.Customers.Include(f => f.PreFactors).ThenInclude(f => f.Factors).Where(f => f.Id == id).FirstOrDefaultAsync();

            await _logSystemService.LogChange(cu, null, LogActionEnum.حذف, user);

            cu.IsDeleted = true;
            cu.UserModify = user.FullName;
            cu.UserModifyID = user.Id;

            foreach (var item in cu.PreFactors ?? [])
            {
                item.IsDeleted = true;
                foreach (var it in item.Factors ?? [])
                {
                    it.IsDeleted = true;
                    it.ModifyDate = DateTime.Now;
                    it.ModifyDateTxt = dtService.GetDateTimePersian();
                    it.UserModify = user.FullName;
                    it.UserModifyID = user.Id;
                }

                item.ModifyDate = DateTime.Now;
                item.ModifyDateTxt = dtService.GetDateTimePersian();
                item.UserModify = user.FullName;
                item.UserModifyID = user.Id;
            }

            cu.ModifyDate = DateTime.Now;
            cu.ModifyDateTxt = dtService.GetDateTimePersian();


            await db.SaveChangesAsync();
            return Ok();
        }


        [Authorize(Roles = "مدیر ارشد ,مدیر فروش")]
        [HttpGet]
        public async Task<IActionResult> AcceptCustomer(Guid id)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var oldCu = await db.Customers.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            var cu = await db.Customers.Where(f => f.Id == id).FirstOrDefaultAsync();

            cu.Accepted = true;
            cu.DateSort = DateTime.Now;
            cu.DateSortTxt = dtService.GetDateTimePersian();
            cu.ModifyDate = DateTime.Now;
            cu.ModifyDateTxt = dtService.GetDateTimePersian();

            var customerText = string.IsNullOrWhiteSpace(cu.CompanyName)
                ? $"{cu.CustomerCode} ({cu.AgentName})"
                : $"{cu.CustomerCode} {cu.CompanyName} ({cu.AgentName})";

            await _logSystemService.LogChange(oldCu, cu, LogActionEnum.تایید_و_تغییر_وضعیت, user);
            await db.SaveChangesAsync();
            pushQueue.Enqueue(new PushQueueItem {
                UserId = cu.UserCreatorID,
                Title = "تایید مشتری",
                Body = $"مشتری {customerText} توسط {user.FullName} تایید شد.",
                Url = "/Customer/Index",
                Tag = "customer" + cu.CustomerCode
            });
            return Ok();
        }

        [Authorize(Policy = "NotCustomerOnly")]
        [HttpGet]
        public async Task<IActionResult> AddToMyCustomer(Guid id)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            var customer = await db.Customers.Where(f => f.Id == id).FirstOrDefaultAsync();
            customer.Accepted = false;


            var uc = await db.UserCustomers.AnyAsync(f => f.UserId == Guid.Parse(user.Id) && f.CustomerId == id);
            if (uc) return Ok(0);


            db.UserCustomers.Add(new UserCustomer {
                CustomerId = id,
                UserId = Guid.Parse(user.Id)
            });
            await db.SaveChangesAsync();
            return Ok(1);
        }

        [Authorize(Policy = "NotCustomerOnly")]
        [HttpPost]
        public async Task<IActionResult> CheckCus(Customer model)
        {
            if (model.Juridical == 1)
            {
                var cus = await db.Customers.Where(f =>
                    f.CompanyName == model.CompanyName ||
                    f.EghtesadiCode == f.EghtesadiCode ||
                    f.NumberRegister == f.NumberRegister ||
                    f.ShenaseMeli == f.ShenaseMeli
                ).ToListAsync();

                if (cus.Any())
                {
                    return Ok(new { Status = 500, error = "مشتری قبلا در سیستم ثبت شده است" });
                } else
                {
                    return Ok(new { Status = 200, error = "" });
                }
            }


            return Ok();
        }

        [Authorize(Policy = "NotCustomerOnly")]
        [HttpPost]
        public async Task<IActionResult> CheckDuplicate([FromBody] Customer model)
        {
            if (model == null) return BadRequest();

            IQueryable<Customer> data;

            if (model.Juridical == 1)
            {
                data = from c in db.Customers
                    where
                        c.AgentName == model.AgentName ||
                        c.CompanyName == model.CompanyName ||
                        c.ShenaseMeli == model.ShenaseMeli ||
                        c.EghtesadiCode == model.EghtesadiCode ||
                        c.NumberRegister == model.NumberRegister
                    select c;
            } else
            {
                data = from c in db.Customers
                    where
                        EF.Functions.Like(c.AgentName, "%" + model.AgentName + "%") ||
                        c.Mobile == model.Mobile
                    select c;
            }

            if (data.Any())
            {
                return PartialView("DuplicatePartial", await data.ToListAsync());
            } else
            {
                return Ok(null);
            }
        }

        [Authorize(Policy = "NotCustomerOnly")]
        [HttpPost]
        public async Task<IActionResult> UpCustomer([FromBody] Customer model)
        {
            try
            {
                var user = userManager.GetUserAsync(HttpContext.User).Result;
                var oldModel = await db.Customers.AsNoTracking().Where(f => f.Id == model.Id).FirstOrDefaultAsync();

                model.RoleTypeTxt = Service.GetTypeTxtRoleType((int)model.RoleType);
                model.Accepted = false;
                model.UserModifyID = user.Id;
                model.UserModify = user.FullName;
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.DateSort = DateTime.Now;
                model.DateSortTxt = dtService.GetDateTimePersian();
                model.NormalizedCompanyName = TextService.NormalizeText(model.CompanyName);
                model.NormalizedAgentName = TextService.NormalizeText(model.AgentName);

                model.CustomerCode = oldModel.CustomerCode;
                model.UserCreator = oldModel.UserCreator;
                model.UserCreatorID = oldModel.UserCreatorID;
                model.CreateDate = oldModel.CreateDate;
                model.CreateDateTxt = oldModel.CreateDateTxt;

                model.Success = await genService.IsCustomerActive(model.Id);
                model.Export = await genService.IsCustomerExport(model.Id);

                db.Customers.Update(model);

                db.Entry(model).Property(f => f.CustomerCode).IsModified = false;
                db.Entry(model).Property(f => f.UserCreator).IsModified = false;
                db.Entry(model).Property(f => f.UserCreatorID).IsModified = false;
                db.Entry(model).Property(f => f.CreateDate).IsModified = false;
                db.Entry(model).Property(f => f.CreateDateTxt).IsModified = false;
                db.Entry(model).Property(f => f.AcquaintanceWay).IsModified = false;
                db.Entry(model).Property(f => f.AcquaintanceWayTxt).IsModified = false;


                var deals = await db.Deals.Where(f => f.CustomerId == model.Id).ToListAsync();
                if (deals.Count != 0)
                {
                    foreach (var deal in deals)
                    {
                        deal.CustomerName = $"{model.CompanyName ?? ""} {model.AgentName ?? ""}".Trim();
                        deal.MobileNumber = model.Mobile;

                        deal.CusCompanyName = model.CompanyName;
                        deal.CusNumberRegister = model.NumberRegister;
                        deal.CusEghtesadiCode = model.EghtesadiCode;
                        deal.CusShenaseMeli = model.ShenaseMeli;
                        deal.CusMelliCode = model.AgentMeliCode;
                        deal.CusPostalCode = model.PostalCode;
                        deal.CusJuridical = model.Juridical;
                        deal.CusAddress = model.Address;
                        deal.CusTel = model.Tel;
                        deal.CusSex = model.Sex;
                        deal.CusProvince = model.Province;
                        deal.CusCity = model.City;
                        deal.CusWorkSenf = model.WorkSenf;
                        deal.CusWorkSenfTxt = model.WorkSenfTxt;
                        deal.CusWorkSenfDescription = model.WorkSenfDescription;
                        deal.CusAcquaintanceWay = model.AcquaintanceWay;
                        deal.CusAcquaintanceWayTxt = model.AcquaintanceWayTxt;

                        await genService.UpdateChildEntityDeal(deal.Id, model);
                    }
                }


                await _logSystemService.LogChange(oldModel, model, LogActionEnum.ویرایش, user);

                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Policy = "NotCustomerOnly")]
        [HttpPost]
        public async Task<IActionResult> AddCustomer([FromBody] Customer model)
        {
            if (model == null) return BadRequest();

            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var setting = await db.ASettings.FirstOrDefaultAsync();
                var user = await userManager.GetUserAsync(HttpContext.User);

                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
                model.RoleTypeTxt = Service.GetTypeTxtRoleType((int)model.RoleType);

                if (setting.SmallOffice) model.Accepted = true;


                /*if (model.Juridical == 1)
                {
                    bool chShenaseMeli = db.Customers.Any(f => f.ShenaseMeli == model.ShenaseMeli);
                    bool chEghtesadiCode = db.Customers.Any(f => f.EghtesadiCode == model.EghtesadiCode);
                    bool chNumberRegister = db.Customers.Any(f => f.NumberRegister == model.NumberRegister);
                }*/

                model.NormalizedCompanyName = TextService.NormalizeText(model.CompanyName);
                model.NormalizedAgentName = TextService.NormalizeText(model.AgentName);


                db.Customers.Add(model);


                var us = new UserCustomer() {
                    UserId = Guid.Parse(user.Id),
                    CustomerId = model.Id
                };
                db.UserCustomers.Add(us);

                await db.SaveChangesAsync();
                await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(model.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                return BadRequest();
            }
        }

        [Authorize(Policy = "NotCustomerOnly")]
        [HttpPost]
        public async Task<ActionResult> GetAllCustomerJuridical()
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
                var dataF = db.Customers.Where(f => f.Juridical == 1).AsQueryable();

                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    if (sortColumnDirection == "asc")
                    {
                        switch (sortColumn)
                        {
                            case "AgentName":
                                dataF = dataF.OrderBy(f => f.AgentName);
                                break;
                            default:
                                dataF = dataF.OrderBy(f => f.CreateDate);
                                break;
                        }
                    } else
                    {
                        switch (sortColumn)
                        {
                            case "AgentName":
                                dataF = dataF.OrderByDescending(f => f.AgentName);
                                break;
                            default:
                                dataF = dataF.OrderByDescending(f => f.CreateDate);
                                break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    dataF = dataF.Where(m =>
                        m.AgentName.Contains(searchValue)
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

        [Authorize(Policy = "NotCustomerOnly")]
        [HttpPost]
        public async Task<ActionResult> GetAllCustomerNonJuridical()
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
                var dataF = db.Customers.Where(f => f.Juridical == 0).AsQueryable();

                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    if (sortColumnDirection == "asc")
                    {
                        switch (sortColumn)
                        {
                            case "AgentName":
                                dataF = dataF.OrderBy(f => f.AgentName);
                                break;
                            default:
                                dataF = dataF.OrderBy(f => f.CreateDate);
                                break;
                        }
                    } else
                    {
                        switch (sortColumn)
                        {
                            case "AgentName":
                                dataF = dataF.OrderByDescending(f => f.AgentName);
                                break;
                            default:
                                dataF = dataF.OrderByDescending(f => f.CreateDate);
                                break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    dataF = dataF.Where(m =>
                        m.AgentName.Contains(searchValue)
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
        /*public async Task<ActionResult> GetAllCustomerPartial(int count)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var rols = await userManager.GetRolesAsync(user);
                IList<Customer> list;

                ViewBag.AccessAccepte = false;

                if (rols.Contains("مدیر ارشد") || rols.Contains("مدیر فروش"))
                {
                    if (count == 0)
                    {
                        var data = db.Customers.OrderBy(f => f.Accepted)
                            .ThenByDescending(f=>f.ModifyDate).AsQueryable();
                        list = await data.ToListAsync();
                    }
                    else
                    {
                        var data = db.Customers.OrderBy(f => f.Accepted)
                            .ThenByDescending(f => f.ModifyDate).AsQueryable();
                        list = await data.Take(count).ToListAsync();
                    }

                    ViewBag.AccessAccepte = true;

                }
                else
                {
                    var customers = await db.UserCustomers
                                    .Where(f => f.UserId == Guid.Parse(user.Id))
                                    .Select(f => f.CustomerId).ToArrayAsync();
                    if (count == 0)
                    {
                        var data = db.Customers
                            .Where(f => customers.Contains(f.Id))
                            .OrderBy(f => f.Accepted)
                            .ThenByDescending(f => f.ModifyDate)
                            .AsQueryable();
                        list = await data.ToListAsync();
                    }
                    else
                    {
                        var data = db.Customers.Where(f => customers.Contains(f.Id))
                           .OrderBy(f => f.Accepted)
                            .ThenByDescending(f=>f.ModifyDate).Take(count).AsQueryable();
                        list = await data.ToListAsync();
                    }
                }
                return PartialView(list);
            }
            catch
            {
                return null;
            }
        }*/

        [Authorize(Policy = "NotCustomerOnly")]
        public async Task<ActionResult> GetAllCustomerPartial(int skip = 0, int take = 10, int roleType = -1, int juridical = -1)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var rols = await userManager.GetRolesAsync(user);
                ViewBag.AccessAccepte = false;
                IQueryable<Customer> query;
                if (rols.Contains("مدیر ارشد") || rols.Contains("مدیر فروش"))
                {
                    query = db.Customers;
                    ViewBag.AccessAccepte = true;
                } else
                {
                    var customers = await db.UserCustomers
                        .Where(f => f.UserId == Guid.Parse(user.Id))
                        .Select(f => f.CustomerId).ToArrayAsync();
                    query = db.Customers.Where(f => customers.Contains(f.Id));
                }

                query = query.OrderBy(f => f.Accepted).ThenByDescending(f => f.ModifyDate);
                if (roleType == 1)
                {
                    query = query.Where(f => f.RoleType == RoleTypeEnum.مشتری || f.RoleType == RoleTypeEnum.تامین_کننده_مشتری);
                } else if (roleType == 2)
                {
                    query = query.Where(f => f.RoleType == RoleTypeEnum.تامین_کننده || f.RoleType == RoleTypeEnum.تامین_کننده_مشتری);
                }

                if (juridical != -1)
                {
                    query = query.Where(f => f.Juridical == juridical);
                }

                var result = await query.Skip(skip).Take(take).Include(f => f.UserCustomers).ToListAsync();

                foreach (var item in result)
                {
                    var uu = item.UserCustomers.Select(d => d.UserId.ToString()).ToArray();

                    var users = await udb.Users.Where(f => uu.Contains(f.Id)).Select(f => f.FullName).ToArrayAsync();

                    item.SelExpertRel = string.Join(" _ ", users);
                }


                return PartialView(result);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> GetUsersRel(IList<UserCustomer> userCustomers)
        {
            var uu = userCustomers.Select(d => d.UserId.ToString()).ToList();

            var users = await udb.Users.AsNoTracking().Where(f => uu.Contains(f.Id)).Select(f => f.FullName).ToArrayAsync();

            return string.Join(" _ ", users);
        }


        public async Task NormalizeAllCustomers()
        {
            var customers = await db.Customers.ToListAsync();

            foreach (var c in customers)
            {
                c.NormalizedAgentName = TextService.NormalizeText(c.AgentName);
                c.NormalizedCompanyName = TextService.NormalizeText(c.CompanyName);
            }

            await db.SaveChangesAsync();
        }

        private static readonly HashSet<string> StopWords = ["از", "در", "به", "که", "با", "را", "و", "یا", "تا", "برای", "این", "آن"];

        [Authorize(Policy = "NotCustomerOnly")]
        public async Task<ActionResult> SearchAllCustomerPartial(string val, int roleType = -1, int juridical = -1)
        {
            try
            {
                val = TextService.NormalizeText(val);

                var keywords = val.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(k => !StopWords.Contains(k)).Distinct().ToList();
                ViewBag.AccessAccepte = true;
                var user = await userManager.GetUserAsync(HttpContext.User);
                var rols = await userManager.GetRolesAsync(user);
                IQueryable<Customer> cusQuery = db.Customers.Include(f => f.UserCustomers);

                if (roleType == 1)
                {
                    cusQuery = cusQuery.Where(f => f.RoleType == RoleTypeEnum.مشتری || f.RoleType == RoleTypeEnum.تامین_کننده_مشتری);
                } else if (roleType == 2)
                {
                    cusQuery = cusQuery.Where(f => f.RoleType == RoleTypeEnum.تامین_کننده || f.RoleType == RoleTypeEnum.تامین_کننده_مشتری);
                }

                if (juridical != -1)
                {
                    cusQuery = cusQuery.Where(f => f.Juridical == juridical);
                }

                if (!(rols.Contains("مدیر ارشد") || rols.Contains("مدیر فروش")))
                {
                    var ids = await db.UserCustomers
                        .Where(u => u.UserId == Guid.Parse(user.Id))
                        .Select(u => u.CustomerId).ToListAsync();

                    cusQuery = cusQuery.Where(c => ids.Contains(c.Id));
                    ViewBag.AccessAccepte = false;
                }

                var data = await cusQuery
                    .Where(c =>
                        keywords.Any(k =>
                            c.NormalizedCompanyName.Contains(k) ||
                            c.NormalizedAgentName.Contains(k)
                        ) || c.CustomerCode.ToString() == val).Select(c => new {
                        Customer = c,
                        Score =
                            (c.NormalizedCompanyName == val ? 100 : 0) +
                            (c.NormalizedAgentName == val ? 100 : 0) +
                            (c.NormalizedCompanyName.StartsWith(val) ? 50 : 0) +
                            (c.NormalizedAgentName.StartsWith(val) ? 50 : 0) +
                            keywords.Count(k =>
                                c.NormalizedAgentName.Contains(k) ||
                                c.NormalizedCompanyName.Contains(k)) * 10
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Customer.ModifyDate)
                    .Select(x => x.Customer)
                    .ToListAsync();

                foreach (var item in data)
                {
                    var uu = item.UserCustomers.Select(d => d.UserId.ToString()).ToArray();

                    var users = await udb.Users.Where(f => uu.Contains(f.Id)).Select(f => f.FullName).ToArrayAsync();

                    item.SelExpertRel = string.Join(" _ ", users);
                }


                return PartialView("GetAllCustomerPartial", data);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public async Task<IActionResult> GetAllCustomers()
        {
            var data = await db.Customers
//                .Where(f => f.RoleType == RoleTypeEnum.مشتری || f.RoleType == RoleTypeEnum.تامین_کننده_مشتری)
                .Select(f => new {
                    f.Id,
                    f.Mobile,
                    f.Tel,
                    f.RoleType,
                    customerName = $"{f.CompanyName} {f.AgentName}".Trim(),
                    customerCode = f.CustomerCode,
                }).ToListAsync();
            return Ok(data);
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public async Task<IActionResult> GetCustomer(Guid id)
        {
            /*var data2 = await db.BaseItems
                .Where(f => f.TypeCode == BaseItemEnum.صنف)
                .Select(g => new SelectOptionGuidDto() {
                    id = g.Id,
                    text = g.Title
                }).ToListAsync();*/
            var data = await db.Customers.Where(f => f.Id == id).FirstOrDefaultAsync();
            return Ok(data);
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public async Task<IActionResult> GetCustomerByMobile(string mobile)
        {
            var data = await db.Customers.Where(f => f.Mobile == mobile).FirstOrDefaultAsync();
            return Ok(data);
        }

        [Authorize(Roles = "مدیر ارشد ,مدیر فروش")]
        [HttpGet]
        public async Task<IActionResult> GetCustomerSelRelation(Guid id)
        {
            var cusUsers = await db.UserCustomers.AsNoTracking().Where(f => f.CustomerId == id).ToListAsync();
            //var cus = await db.Customers.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            var user = await udb.Users.ToListAsync();

            var data = (from _user in user
                join cUser in cusUsers
                    on _user.Id equals cUser.UserId.ToString()
                select _user).ToList();


            return Ok(data);
        }


        [Authorize(Policy = "NotCustomerOnly")]
        [HttpGet]
        public async Task<IActionResult> GetSepidarFactors(int pfSerialNumber, int passType = 1)
        {
            try
            {
                var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();

                if (setting?.Sepidar != true) return NoContent();

                var factors = await sepidarService.GetInvoicesByQuotationNumberAsync(pfSerialNumber);

                if (passType == 1)
                {
                    foreach (var factor in factors)
                    {
                        factor.ExtraDescription = sepidarService.getNoteInvoice(factor.InvoiceId);
                    }
                }

                var html = await vrs.RenderToStringAsync("GetSepidarFactors",
                    factors,
                    new Dictionary<string, object> {
                        { "PassType", passType },
                    }
                );

                return Ok(new { count = factors.Count, html });
            }
            catch
            {
                return BadRequest();
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAllPreFactorPartial(Guid? customerId, Guid? dealId, bool showDeleted = false)
        {
            try
            {
                var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();
                var taminUsers = await userManager.GetUsersInRoleAsync("کارشناس تامین");
                ViewBag.PassType = dealId == null ? 1 : 4;

                var query = preFactorService.BuildQuery();

                if (showDeleted && !User.IsCustomer())
                {
                    query = query.IgnoreQueryFilters();
                }

                if (dealId.HasValue)
                {
                    query = query.Where(x => x.DealId == dealId);
                } else if (customerId.HasValue)
                {
                    query = query.Where(x => x.CustomerId == customerId);
                } else
                {
                    return Ok(new { count = 0, html = string.Empty });
                }

                var data = await query.AsNoTracking().ToListAsync();

                await preFactorService.FillDetailsAsync(data);

                var model = new PrefactorViewModel {
                    data = data,
                    UserTamin = taminUsers,
                    _Setting = setting
                };

                var html = await vrs.RenderToStringAsync("GetAllPreFactorPartial", model,
                    new Dictionary<string, object> {
                        { "PassType", ViewBag.PassType }
                    });

                return Ok(new { count = data.Count, html });
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        [Authorize(Roles = "مدیر ارشد")]
        [HttpGet]
        public async Task<IActionResult> GetUsersForPreFactor(Guid cusId, Guid pfId)
        {
            var pf = await db.PreFactors.Where(x => x.Id == pfId).Select(x => new {
                x.UserCreatorID,
                x.UserTaminId
            }).FirstOrDefaultAsync();

            if (pf == null) return NotFound("رکوردی یافت نشد!");

            List<SelectOptionDto<Guid>> salesUsers;
            if (cusId != Guid.Empty)
            {
                var cusUsers = await db.UserCustomers
                    .Where(x => x.CustomerId == cusId)
                    .Select(x => x.UserId.ToString())
                    .ToListAsync();


                salesUsers = await udb.Users
                    .Where(x => cusUsers.Contains(x.Id))
                    .Select(x => new SelectOptionDto<Guid>() {
                        Value = Guid.Parse(x.Id),
                        Text = x.FullName,
                        Target = x.Id == pf.UserCreatorID
                    })
                    .ToListAsync();
            } else
            {
                salesUsers = (await userManager.GetUsersInRoleAsync(AppRoles.SalesExpert))
                    .Select(x => new SelectOptionDto<Guid> {
                        Value = Guid.Parse(x.Id),
                        Text = x.FullName,
                        Target = x.Id == pf.UserCreatorID
                    })
                    .ToList();

                if (!salesUsers.Any(x => x.Value.ToString() == pf.UserCreatorID))
                {
                    var currentUser = await udb.Users
                        .Where(x => x.Id == pf.UserCreatorID)
                        .Select(x => new { x.Id, x.FullName })
                        .FirstOrDefaultAsync();

                    if (currentUser != null)
                    {
                        salesUsers.Insert(0, new SelectOptionDto<Guid> {
                            Value = Guid.Parse(currentUser.Id),
                            Text = currentUser.FullName,
                            Target = true
                        });
                    }
                }
            }

            var taminUsers = (await userManager.GetUsersInRoleAsync(AppRoles.TaminExpert))
                .Select(x => new SelectOptionDto<Guid> {
                    Value = Guid.Parse(x.Id),
                    Text = x.FullName,
                    Target = Guid.Parse(x.Id) == pf.UserTaminId
                })
                .ToList();

            return Ok(new {
                SalesUsers = salesUsers,
                TaminUsers = taminUsers
            });
        }

        [Authorize(Roles = "مدیر ارشد")]
        [HttpGet]
        public async Task<IActionResult> ChangeUsersPreFactor(Guid pfId, Guid userId, Guid taminUserId)
        {
            var pf = await db.PreFactors.FirstOrDefaultAsync(x => x.Id == pfId);

            if (pf == null) return NotFound("رکوردی یافت نشد!");

            if (taminUserId != Guid.Empty)
            {
                var taminUser = await udb.Users
                    .Where(x => x.Id == taminUserId.ToString())
                    .Select(x => new {
                        x.Id,
                        x.FullName
                    }).FirstOrDefaultAsync();

                if (taminUser == null) return BadRequest();

                pf.UserTaminId = taminUserId;
                pf.UserTaminTxt = taminUser.FullName;
            }

            if (userId != Guid.Empty)
            {
                var salesUser = await udb.Users
                    .Where(x => x.Id == userId.ToString())
                    .Select(x => new {
                        x.Id,
                        x.FullName
                    }).FirstOrDefaultAsync();

                if (salesUser == null) return BadRequest();

                pf.UserCreatorID = userId.ToString();
                pf.UserCreator = salesUser.FullName;


                var factors = await db.Factors.Where(x => x.PfactorID == pf.Id).ToListAsync();
                foreach (var factor in factors)
                {
                    factor.SelerFullName = salesUser.FullName;
                    factor.SelerId = userId.ToString();
                }
            }

            await db.SaveChangesAsync();

            return Ok();
        }

        [Authorize(Policy = "NotCustomerOnly")]
        [HttpGet]
        public IActionResult GetPhonebook(string search = "")
        {
            var query = db.Customers
                .Select(c => new {
                    c.Id,
                    Name = c.CompanyName + " " + c.AgentName,
                    phone = c.Tel,
                    c.CustomerCode,
                    c.Mobile,
                    c.ModifyDate,
                    c.NormalizedAgentName,
                    c.NormalizedCompanyName
                });

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = TextService.NormalizeText(search);
                var keywords = search.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(k => !StopWords.Contains(k)).Distinct().ToList();

                query = query
                    .Where(c =>
                        keywords.Any(k =>
                            c.NormalizedCompanyName.Contains(k) ||
                            c.NormalizedAgentName.Contains(k) ||
                            c.phone.Contains(search) || c.Mobile.Contains(search)
                        ) || c.CustomerCode.ToString() == search
                    ).Select(c => new {
                        Customer = c,
                        Score =
                            (c.NormalizedCompanyName == search ? 100 : 0) +
                            (c.NormalizedAgentName == search ? 100 : 0) +
                            (c.NormalizedCompanyName.StartsWith(search) ? 50 : 0) +
                            (c.NormalizedAgentName.StartsWith(search) ? 50 : 0) +
                            keywords.Count(k => c.NormalizedAgentName.Contains(k) || c.NormalizedCompanyName.Contains(k)) * 10
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Customer.ModifyDate)
                    .Select(x => x.Customer);
            } else
            {
                query = query.OrderByDescending(c => c.ModifyDate).Take(50);
            }

            var data = query.ToList();

            return Ok(data);
        }
    }
}