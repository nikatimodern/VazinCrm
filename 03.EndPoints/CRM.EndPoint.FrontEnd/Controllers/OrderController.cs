using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Orders.Entities;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.PrintViewModel;
using CRM.EndPoint.FrontEnd.Services;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Supply;
using CRM.EndPoint.FrontEnd.Services.Factor;
using CRM.EndPoint.FrontEnd.Services.QueryExtensions;
using DocumentFormat.OpenXml.Spreadsheet;


namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize(Policy = "NotCustomerOnly")]
    public class OrderController : Controller {
        private readonly IVeiwRenderService vrs;
        private readonly Service genService;
        private readonly FileSaver fileSaver;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly UserContextDb udb;
        private readonly UserManager<AppUser> userManager;
        private readonly SmsServices smsServ;
        private readonly LogSystemService _logSystemService;

        public OrderController(IVeiwRenderService vrs, Service GenService, FileSaver fileSaver, DateTimeServices dtService,
            CRMContextDb db, UserContextDb udb, UserManager<AppUser> userManager, SmsServices SmsServ, LogSystemService logSystemService)
        {
            this.vrs = vrs;
            genService = GenService;
            this.fileSaver = fileSaver;
            this.dtService = dtService;
            this.db = db;
            this.udb = udb;
            this.userManager = userManager;
            smsServ = SmsServ;
            _logSystemService = logSystemService;
        }

        public IActionResult Index( /*Guid? CuId,Guid? PfId*/)
        {
            return View();
        }

        public IActionResult Orders(Guid id)
        {
            ViewBag.headOrId = id;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderHead(Guid pfid)
        {
            ViewBag.passType = 1;
            var model = await db.OrderHeads.Where(f => f.Prefactor == pfid).ToListAsync();
            return PartialView(model);
        }


        public async Task<IActionResult> GetPrefactorOp(Guid id)
        {
            var data = await db.PreFactors
                .Where(f => f.CustomerId == id && f.Pftype == (int)PreFactorEnum.در_انتظار_تولید)
                .Select(f => new SelectOptionGuidDto() { id = f.Id, text = f.SerialNumber.ToString() })
                .ToListAsync();

            return Ok(data);
        }

        public async Task<IActionResult> GetFactorOp(Guid id)
        {
            List<SelectOptionGuidDto> fact = null;
            if (id == Guid.Empty)
            {
                fact = await db.Factors
                    .Where(f => f.CustomerId == id)
                    .Select(f => new SelectOptionGuidDto() { id = f.Id, text = f.SerialNumber.ToString() })
                    .ToListAsync();
            } else
            {
                fact = await db.Factors
                    .Where(f => f.CustomerId == id)
                    .Select(f => new SelectOptionGuidDto() { id = f.Id, text = f.SerialNumber.ToString() })
                    .ToListAsync();
            }


            return Ok(fact);
        }

        public async Task<IActionResult> GetCustomersOpId()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var UserRols = await userManager.GetRolesAsync(user);


            Guid[] usCu;

            if (UserRols.Contains("مدیر فروش")
                || UserRols.Contains("کارشناس مالی")
                || UserRols.Contains("مدیر مالی")
                || UserRols.Contains("مدیر ارشد"))
            {
                usCu = await db.UserCustomers.Select(f => f.CustomerId).ToArrayAsync();
            } else
            {
                usCu = await db.UserCustomers.Where(f => f.UserId == Guid.Parse(user.Id)).Select(f => f.CustomerId).ToArrayAsync();
            }


            var data = await db.Customers.Where(f =>
                usCu.Contains(f.Id) && (f.RoleType == RoleTypeEnum.مشتری || f.RoleType == RoleTypeEnum.تامین_کننده_مشتری)
            ).Select(g => new SelectOptionGuidDto {
                id = g.Id,
                text = $"{g.CompanyName} {g.AgentName} _ (کد مشتری  {g.CustomerCode})"
            }).ToListAsync();
            return Ok(data);
        }


        [HttpPost]
        public async Task<IActionResult> UpHeadDescription(Guid Id, string Description)
        {
            var data = await db.OrderHeads.Where(f => f.Id == Id).FirstOrDefaultAsync();
            data.Description = Description;
            await db.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> PreviewPrint(Guid id)
        {
            var com = await db.Companies.Include(f => f.CompanyFiles).FirstOrDefaultAsync();
            var order = await db.OrderHeads.Where(f => f.Id == id).Include(f => f.Orders).FirstOrDefaultAsync();


            if (order == null)
            {
                HttpContext.Items["ErrorMessage"] = "حواله تولیدی پیدا نشد!";
                return NotFound();
            }

            var pf = await db.PreFactors.IgnoreQueryFilters().Where(f => f.Id == order.Prefactor).FirstOrDefaultAsync();

            if (pf == null)
            {
                return NotFound("پیش فاکتور حذف شده است");
            }

            var ss = new OrederPrintViewModel() {
                Company = com,
                OrderHead = order,
                OfficialType = pf.OfficialType
            };

            if (com.CompanyFiles.Where(f => f.Filename == "logo").ToList().Any())
            {
                ss.Logo = com.CompanyFiles.FirstOrDefault(f => f.Filename == "logo").FileUrl;
            }

            return View(ss);
        }


        [HttpGet]
        public async Task<IActionResult> ChangeStatusHd(Guid id, int status, bool sendSms)
        {
            try
            {
                var oldHd = await db.OrderHeads.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
                var hd = await db.OrderHeads.Where(f => f.Id == id).FirstOrDefaultAsync();
                var user = userManager.GetUserAsync(HttpContext.User).Result;

                var roles = userManager.GetRolesAsync(user).Result.ToArray();

                if (user.Id == hd.UserCreatorID || user.Id == hd.UserTaminId.ToString()
                                                || roles.Contains("مدیر ارشد") || roles.Contains("مدیر فروش") || roles.Contains("مدیر تامین"))
                {
                    hd.Status = (OrderHeadStatusEnum)status;
                    hd.ModifyDate = DateTime.Now;
                    hd.ModifyDateTxt = dtService.GetDateTimePersian();
                    hd.UserModify = user.FullName;
                    hd.UserModifyID = user.Id;

                    await _logSystemService.LogChange(oldHd, hd, LogActionEnum.تغییر_وضعیت, user);

                    if (status == 3 && sendSms)
                    {
                        var cusMobile = "";
                        if (hd.Customer != null)
                        {
                            var cus = await db.Customers.Where(f => f.Id == hd.Customer).FirstOrDefaultAsync();
                            cusMobile = cus.Mobile;
                        } else if (hd.DealId != null)
                        {
                            var deal = await db.Deals.Where(f => f.Id == hd.DealId).FirstOrDefaultAsync();
                            cusMobile = deal.MobileNumber;
                        }

                        await smsServ.SendReadyToDeliverToCustomer(cusMobile, hd.PrefactorSer);
                    }

                    await db.SaveChangesAsync();
                    return Ok();
                }

                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetAllHeadOrdersPartial([FromBody] filterVModel model, [FromQuery] int fStatus = -1)
        {
            try
            {
                ViewBag.PassType = 1;
                var user = await userManager.GetUserAsync(HttpContext.User);
                var rols = await userManager.GetRolesAsync(user);

                var dataQ = db.OrderHeads.AsQueryable();

                dataQ = dataQ.Where(item =>
                    (string.IsNullOrEmpty(model.serVal)
                     || item.OrderNumber.ToString() == model.serVal
                     || item.CustomerTxt.Contains(model.serVal)
                     || item.PrefactorSer.ToString() == model.serVal
                     || item.CustomerCode.ToString() == model.serVal)
                    && item.Status == (OrderHeadStatusEnum)model._type
                );

                if (fStatus != -1)
                {
                    var preFactorsHasFactorIds = await db.PreFactors.Where(f => f.Factors.Count > 0).Select(f => f.Id).ToListAsync();

                    if (fStatus == 1)
                        dataQ = dataQ.Where(f => preFactorsHasFactorIds.Contains(f.Prefactor.Value));
                    else if (fStatus == 2)
                        dataQ = dataQ.Where(f => !preFactorsHasFactorIds.Contains(f.Prefactor.Value));
                }

                if (model.filt != null)
                {
                    dataQ = dataQ.ApplyFilter(model, f => f.Customer, f => f.OrderDateMildi, f => f.UserCreatorID);
                }

                //اگر فیلتر نداشت پیش فرض سفارش هایی که از این تاریخ به بعد هستند رو نمایش میده
                var date = dtService.GetDateMiladi("1404/01/01");

                bool hasFilter = model.filt != null;
                bool hasSearch = !string.IsNullOrEmpty(model.serVal);

                if (rols.Contains("مدیر ارشد") || rols.Contains("مدیر فروش") || rols.Contains("کارشناس مالی") || rols.Contains("مدیر مالی"))
                {
                    if (!hasFilter && !hasSearch)
                        dataQ = dataQ.Where(f => f.OrderDateMildi > date).Take(200);
                } else if (rols.Contains("کارشناس تامین"))
                {
                    dataQ = dataQ.Where(f => f.UserCreatorID == user.Id || f.UserTaminId.ToString() == user.Id);

                    if (hasFilter)
                        dataQ = dataQ.Take(200);
                    else if (!hasSearch)
                        dataQ = dataQ.Where(f => f.OrderDateMildi > date).Take(200);
                } else
                {
                    dataQ = dataQ.Where(f => f.UserCreatorID == user.Id);

                    if (!hasFilter && !hasSearch)
                        dataQ = dataQ.Where(f => f.OrderDateMildi > date).Take(200);
                }


                var data = await dataQ.OrderByDescending(f => f.CreateDate).ToListAsync();


                var html = await vrs.RenderToStringAsync("GetAllHeadOrdersPartial", data,
                    new Dictionary<string, object> {
                        { "PassType", ViewBag.PassType }
                    });

                return Ok(new { count = data.Count, html });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException);
            }
        }


        [HttpPost]
        public async Task<IActionResult> getAllOrders(Guid id)
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
                var dataF = db.Orders.Where(f => f.OrderHeadId == id).AsQueryable();

                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    if (sortColumnDirection == "asc")
                    {
                        switch (sortColumn)
                        {
                            case "CreateDate":
                                dataF = dataF.OrderBy(f => f.CreateDate);
                                break;
                            default:
                                dataF = dataF.OrderBy(f => f.CreateDate);
                                break;
                        }
                    } else
                    {
                        switch (sortColumn)
                        {
                            case "CreateDate":
                                dataF = dataF.OrderByDescending(f => f.CreateDate);
                                break;
                            default:
                                dataF = dataF.OrderByDescending(f => f.CreateDate);
                                break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    //dataF = dataF.Where(
                    //    m =>
                    //    m.OrderNumber == Int32.Parse(searchValue)
                    //    //m.AgentName.Contains(searchValue)
                    //);
                }

                recordsTotal = dataF.Count();
                var data = await dataF.Skip(skip).Take(pageSize).ToListAsync();
                var SCount = skip + 1;
                for (int i = 0; i < data.Count(); i++)
                {
                    data[i].Row = SCount++;
                }

                var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
                return Ok(jsonData);
            }
            catch
            {
                return null;
            }
        }


        [HttpPost]
        public async Task<IActionResult> addOrder([FromBody] Order model)
        {
            var user = userManager.GetUserAsync(HttpContext.User).Result;
            await db.Orders.AddAsync(model);


            await db.SaveChangesAsync();
            return Ok();
        }


        [HttpGet]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var data = await db.Orders.Where(f => f.Id == id).FirstOrDefaultAsync();
            return Ok(data);
        }

        public async Task<List<PreFactorItem>> CompareOrdersAndPfItems(Guid pfId)
        {
            var orders = await db.OrderHeads.Where(f => f.Prefactor == pfId).SelectMany(f => f.Orders).ToListAsync();
            var pfItems = await db.PreFactorItems.Where(f => f.PrefactorId == pfId && f.IsInProduction).ToListAsync();

            var ordersPfItemIds = orders.Select(f => f.PreFactorItemId).Distinct().ToList();

            var pfItemsNotInOrders = pfItems.Where(f => !ordersPfItemIds.Contains(f.Id)).ToList();

            return pfItemsNotInOrders;
        }

        public async Task<IActionResult> GetHeadOrder(Guid id)
        {
            try
            {
                var orderHead = await db.OrderHeads.Where(f => f.Id == id).Include(f => f.Orders).FirstOrDefaultAsync();

                var pfItemsNotInOrders = orderHead?.Prefactor != null ? await CompareOrdersAndPfItems(orderHead.Prefactor.Value) : [];

                var model = new { orderHead, pfItemsNotInOrders };

                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public async Task<IActionResult> GetHeadOrdersByPfId(Guid pfId)
        {
            try
            {
                var orderHeads = await db.OrderHeads.Where(f => f.Prefactor == pfId).Include(f => f.Orders).ToListAsync();

                var pfItemsNotInOrders = orderHeads.Count != 0 ? await CompareOrdersAndPfItems(orderHeads[0].Prefactor.Value) : [];

                var model = new { orderHeads, pfItemsNotInOrders };

                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUndeliveredOrderHeads(Guid pfId)
        {
            try
            {
                ViewBag.passType = 2;
                var orderHeads = await db.OrderHeads.Where(f => f.Prefactor == pfId && f.Status != OrderHeadStatusEnum.تحویل_شده).ToListAsync();
                return PartialView("GetOrderHead", orderHeads);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllOrderHeadsByCurrentTamin()
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var roles = await userManager.GetRolesAsync(user);


                var orderHeadQ = db.OrderHeads.AsQueryable();

                if (!roles.Contains("مدیر ارشد") && !roles.Contains("مدیر فروش") && !roles.Contains("کارشناس مالی") && !roles.Contains("مدیر مالی"))
                {
                    orderHeadQ = roles.Contains("کارشناس تامین")
                        ? orderHeadQ.Where(f => f.UserCreatorID == user.Id || f.UserTaminId.ToString() == user.Id)
                        : orderHeadQ.Where(f => f.UserCreatorID == user.Id);
                }

                var data = await orderHeadQ.Select(f => f.Id).ToListAsync();

                /*
                var pfs = db.PreFactors
                    .Where(f => f.UserTaminId == Guid.Parse(CUser.Id))
                    .Select(f => f.Id)
                    .ToArray();

                var orderHead = await db.OrderHeads
                    .Where(f => pfs.Contains(f.Prefactor.Value))
                    .Select(f => f.Id).ToListAsync();*/


                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GetAllOrderHeads([FromBody] GetAllBySelectedVModel model)
        {
            try
            {
                ViewBag.passType = model.PassType;
                var search = model.Search;

                var query = db.OrderHeads.AsNoTracking().AsQueryable();

                if (model.FilterIds != null)
                {
                    query = query.Where(f => model.FilterIds.Contains(f.Id));
                }

                query = query
                    .OrderByDescending(f => model.SelectedIds.Contains(f.Id))
                    .ThenByDescending(f => f.CreateDate);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(f =>
                        f.CustomerTxt.Contains(search) ||
                        f.OrderNumber == TextService.convertToInt(search) ||
                        f.PrefactorSer == TextService.convertToInt(search) ||
                        f.CustomerCode == TextService.convertToInt(search)
                    );
                } else
                {
                    query = query.Take(50);
                }

                var orderHeads = await query.ToListAsync();
                return PartialView("GetOrderHead", orderHeads);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /*[HttpGet]
        public async Task<IActionResult> GetHeadOrder(Guid id)
        {
            try
            {
                var data = await db.OrderHeads.Where(f => f.Id == id).Include(x => x.Orders)
                    .Select(h => new {
                        h.Id,
                        h.CustomerTxt,
                        h.CustomerCode,
                        h.UserCreator,
                        h.UserTaminTxt,
                        h.Description,
                        h.OrderNumber,
                        h.PrefactorSer,
                        h.OrderDate,
                        h.Tel,
                        Orders = h.Orders.Select(o => new {
                            o.Id,
                            o.ProductSmallName,
                            o.Dimensions,
                            o.CategoryType,
                            o.Caset,
                            o.BackgroundColor,
                            o.PrintColor,
                            o.CountColor,
                            o.DesignName,
                            o.Weight,
                            o.UnitTxt,
                            o.Thickness,
                            o.PreFactorItemId,
                            o.IsDeleted,
                        }).ToList()
                    }).FirstOrDefaultAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }*/


        [HttpPost]
        public async Task<IActionResult> UpOrder(Order model)
        {
            try
            {
                var user = userManager.GetUserAsync(HttpContext.User).Result;

                var oldOr = await db.Orders.AsNoTracking().Where(f => f.Id == model.Id).FirstOrDefaultAsync();
                var or = await db.Orders.Where(f => f.Id == model.Id).FirstOrDefaultAsync();

                db.Entry(or).CurrentValues.SetValues(model);

                db.Entry(or).Property(f => f.CreateDate).IsModified = false;
                db.Entry(or).Property(f => f.CreateDateTxt).IsModified = false;

                or.UserModify = user.FullName;
                or.UserModifyID = user.Id;
                or.ModifyDate = DateTime.Now;
                or.ModifyDateTxt = dtService.GetDateTimePersian();

                await _logSystemService.LogChange(oldOr, or, LogActionEnum.ویرایش, user);
                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpHeadOrder([FromBody] OrderHead model)
        {
            try
            {
                var user = userManager.GetUserAsync(HttpContext.User).Result;
                var oldData = await db.OrderHeads.AsNoTracking().Where(f => f.Id == model.Id).FirstOrDefaultAsync();
                var data = await db.OrderHeads.Where(f => f.Id == model.Id).Include(f => f.Orders).FirstOrDefaultAsync();

                var roles = userManager.GetRolesAsync(user).Result.ToArray();
                if (user.Id == model.UserCreatorID || user.Id == model.UserTaminId.ToString() || roles.Contains("مدیر ارشد") ||
                    roles.Contains("مدیر فروش") || roles.Contains("مدیر تامین"))
                {
                    data.Tel = model.Tel;
                    data.OrderDate = model.OrderDate;
                    data.Description = model.Description;
                    data.UserModify = user.FullName;
                    data.UserModifyID = user.Id;
                    data.ModifyDate = DateTime.Now;
                    data.ModifyDateTxt = dtService.GetDateTimePersian();

                    db.Entry(data).Property(f => f.CreateDate).IsModified = false;
                    db.Entry(data).Property(f => f.CreateDateTxt).IsModified = false;
                    db.Entry(data).Property(f => f.OrderNumber).IsModified = false;

                    foreach (var item in model.Orders)
                    {
                        if (item.Id != Guid.Empty)
                        {
                            var orderItem = data.Orders.FirstOrDefault(o => o.Id == item.Id);
                            db.Entry(orderItem).CurrentValues.SetValues(item);
                        } else
                        {
                            data.Orders.Add(item);
                        }
                    }


                    await _logSystemService.LogChange(oldData, data, LogActionEnum.ویرایش, user, ["Orders"]);

                    await db.SaveChangesAsync();
                    return Ok();
                }

                return NoContent();
            }
            catch
            {
                return NoContent();
            }
        }


        [HttpPost]
        public async Task<IActionResult> getAllHeadOrders()
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
                var dataF = db.OrderHeads.AsQueryable();

                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    if (sortColumnDirection == "asc")
                    {
                        switch (sortColumn)
                        {
                            case "OrderNumber":
                                dataF = dataF.OrderBy(f => f.OrderNumber);
                                break;
                            default:
                                dataF = dataF.OrderBy(f => f.CreateDate);
                                break;
                        }
                    } else
                    {
                        switch (sortColumn)
                        {
                            case "OrderNumber":
                                dataF = dataF.OrderByDescending(f => f.OrderNumber);
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
                        //m.CustomerTxt == searchValue
                        m.CustomerTxt.Contains(searchValue)
                    );
                }

                recordsTotal = dataF.Count();
                List<OrderHead> data = new List<OrderHead>();
                if (pageSize == -1)
                    data = await dataF.ToListAsync();
                else
                    data = await dataF.Skip(skip).Take(pageSize).ToListAsync();
                var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
                return Ok(jsonData);
            }
            catch
            {
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> DelOrder(Guid id)
        {
            try
            {
                var user = userManager.GetUserAsync(HttpContext.User).Result;
                var oh = await db.Orders.Where(f => f.Id == id).FirstOrDefaultAsync();

                await _logSystemService.LogChange(oh, null, LogActionEnum.حذف, user);

                oh.IsDeleted = true;
                oh.UserModify = user.FullName;
                oh.UserModifyID = user.Id;
                oh.ModifyDate = DateTime.Now;
                oh.ModifyDateTxt = dtService.GetDateTimePersian();

                await db.SaveChangesAsync();
            }
            catch { }

            return Ok();
        }


        [HttpGet]
        public async Task<IActionResult> DelOrderHead(Guid id)
        {
            try
            {
                var user = userManager.GetUserAsync(HttpContext.User).Result;
                var oh = await db.OrderHeads.Where(f => f.Id == id).FirstOrDefaultAsync();
                await _logSystemService.LogChange(oh, null, LogActionEnum.حذف, user);
                var roles = userManager.GetRolesAsync(user).Result.ToArray();
                if (user.Id == oh.UserCreatorID || user.Id == oh.UserTaminId.ToString() || roles.Contains("مدیر ارشد") ||
                    roles.Contains("مدیر فروش") || roles.Contains("مدیر تامین"))
                {
                    oh.IsDeleted = true;
                    oh.UserModify = user.FullName;
                    oh.UserModifyID = user.Id;
                    oh.ModifyDate = DateTime.Now;
                    oh.ModifyDateTxt = dtService.GetDateTimePersian();
                    await db.SaveChangesAsync();
                    return Ok();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> AddHeadOrder([FromBody] List<OrderHead> models)
        {
            try
            {
                var user = userManager.GetUserAsync(HttpContext.User).Result;
                var deal = await db.Deals.Where(f => f.Id == models.First().DealId).FirstOrDefaultAsync();

                foreach (var model in models)
                {
                    model.OrderDateMildi = dtService.GetDateMiladi(model.OrderDate);
                    model.UserCreator = user.FullName;
                    model.UserCreatorID = user.Id;
                }


                db.OrderHeads.UpdateRange(models);

                foreach (var model in models)
                {
                    db.Entry(model).Property(f => f.OrderNumber).IsModified = false;
                    if (model.OrderNumber == 0)
                    {
                        await db.SaveChangesAsync();
                        await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
                    } else
                    {
                        var oldData = await db.OrderHeads.AsNoTracking().Include(f => f.Orders).Where(f => f.Id == model.Id)
                            .FirstOrDefaultAsync();
                        await _logSystemService.LogChange(oldData, model, LogActionEnum.ویرایش, user, ["Orders"]);
                    }
                }

                if (deal != null)
                {
                    deal.ModifyDate = DateTime.Now;
                    deal.ModifyDateTxt = dtService.GetDateTimePersian();
                }

                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public async Task<IActionResult> GetAllDealOrdersPartial(Guid dealId)
        {
            ViewBag.PassType = 4;

            var orders = db.OrderHeads.Where(f => f.DealId == dealId).AsQueryable();

            var orderRegistered = orders.Where(f => f.Status == OrderHeadStatusEnum.ثبت_اولیه).ToList();
            var orderProducing = orders.Where(f => f.Status == OrderHeadStatusEnum.تولید).ToList();
            var orderFinished = orders.Where(f => f.Status == OrderHeadStatusEnum.آماده_تحویل).ToList();
            var orderDelivered = orders.Where(f => f.Status == OrderHeadStatusEnum.تحویل_شده).ToList();


            var registeredHtml = await vrs.RenderToStringAsync("GetAllHeadOrdersPartial", orderRegistered,
                new Dictionary<string, object> { { "PassType", ViewBag.PassType } }
            )!;

            var producingHtml = await vrs.RenderToStringAsync("GetAllHeadOrdersPartial", orderProducing,
                new Dictionary<string, object> { { "PassType", ViewBag.PassType } }
            )!;

            var finishedHtml = await vrs.RenderToStringAsync("GetAllHeadOrdersPartial", orderFinished,
                new Dictionary<string, object> { { "PassType", ViewBag.PassType } }
            )!;

            var deliveredHtml = await vrs.RenderToStringAsync("GetAllHeadOrdersPartial", orderDelivered,
                new Dictionary<string, object> { { "PassType", ViewBag.PassType } }
            )!;


            return Ok(new {
                dealOrderRegistered = new { count = orderRegistered.Count, html = registeredHtml },
                dealOrderProducing = new { count = orderProducing.Count, html = producingHtml },
                dealOrderFinished = new { count = orderFinished.Count, html = finishedHtml },
                dealOrderDelivered = new { count = orderDelivered.Count, html = deliveredHtml }
            });
        }
    }
}