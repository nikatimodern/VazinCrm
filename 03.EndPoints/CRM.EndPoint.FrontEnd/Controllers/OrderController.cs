using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Orders.Entities;
using CRM.EndPoint.FrontEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.EndPoint.FrontEnd.IServices.DocumentRender;
using CRM.EndPoint.FrontEnd.IServices.Sms;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.DocumentRender;
using CRM.EndPoint.FrontEnd.Models.Order;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Models.Supply;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.Infrastructures.Sql;
using Microsoft.Playwright;


namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize(Policy = "NotCustomerOnly")]
    public class OrderController : Controller {
        private readonly IViewRenderService vrs;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly UserManager<AppUser> userManager;
        private readonly LogSystemService _logSystemService;
        private readonly ISmsQueue smsQueue;
        private readonly IDocumentRenderService documentRender;

        public OrderController(IViewRenderService vrs, DateTimeServices dtService,
            CRMContextDb db, UserManager<AppUser> userManager, LogSystemService logSystemService,
            ISmsQueue smsQueue, IDocumentRenderService documentRender)
        {
            this.vrs = vrs;
            this.dtService = dtService;
            this.db = db;
            this.userManager = userManager;
            _logSystemService = logSystemService;
            this.smsQueue = smsQueue;
            this.documentRender = documentRender;
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
                .Select(f => new SelectOptionIdDto() { id = f.Id, text = f.SerialNumber.ToString() })
                .ToListAsync();

            return Ok(data);
        }

        public async Task<IActionResult> GetFactorOp(Guid id)
        {
            List<SelectOptionIdDto> fact = null;
            if (id == Guid.Empty)
            {
                fact = await db.Factors
                    .Where(f => f.CustomerId == id)
                    .Select(f => new SelectOptionIdDto() { id = f.Id, text = f.SerialNumber.ToString() })
                    .ToListAsync();
            } else
            {
                fact = await db.Factors
                    .Where(f => f.CustomerId == id)
                    .Select(f => new SelectOptionIdDto() { id = f.Id, text = f.SerialNumber.ToString() })
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
            ).Select(g => new SelectOptionIdDto {
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
                HttpContext.Items["ErrorMessage"] = "حواله تولیدی یافت نشد!";
                return NotFound();
            }

            var pf = await db.PreFactors.IgnoreQueryFilters().Where(f => f.Id == order.Prefactor).FirstOrDefaultAsync();

            if (pf == null)
            {
                return NotFound("پیش فاکتور حذف شده است");
            }

            var ss = new OrderPrintViewModel() {
                Company = com,
                OrderHead = order,
                OfficialType = pf.OfficialType,
                SalesExpert = pf.UserCreator
            };

            if (com.CompanyFiles.Where(f => f.Filename == "logo").ToList().Any())
            {
                ss.Logo = com.CompanyFiles.FirstOrDefault(f => f.Filename == "logo").FileUrl;
            }

            return View(ss);
        }


        public async Task<IActionResult> DownloadOrderPdf(Guid id)
        {
            try
            {
                var url = Url.Action(nameof(PreviewPrint), "Order", new { id }, Request.Scheme);
                var pdf = await documentRender.GeneratePdfAsync(Request, url,
                    new PagePdfOptions { Landscape = true, Format = PaperFormat.A5 });

                return File(pdf, "application/pdf", "Order.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public async Task<IActionResult> DownloadOrderImage(Guid id)
        {
            try
            {
                var url = Url.Action(nameof(PreviewPrint), "Order", new { id }, Request.Scheme);
                var image = await documentRender.GenerateImageAsync(Request, url, new DocumentRenderImageOptions {
                        Viewport = ViewportPresets.A5Landscape,
                        Selectors = [".sheet"]
                    }
                );

                return File(image, "image/png", "Order.png");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        public async Task<IActionResult> ChangeStatusHd(Guid id, int status, bool sendSms)
        {
            try
            {
                var oldHd = await db.OrderHeads.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
                var hd = await db.OrderHeads.FirstOrDefaultAsync(f => f.Id == id);
                var user = userManager.GetUserAsync(HttpContext.User).Result;

                var roles = await userManager.GetRolesAsync(user);

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

                        // await smsServ.SendReadyToDeliverToCustomer(cusMobile, hd.PrefactorSer);
                        await smsQueue.QueueAsync(new SmsJob {
                            Type = SmsType.ReadyToDeliverToCustomer,
                            Mobile = cusMobile,
                            SerialNumber = hd.PrefactorSer.ToString(),
                        });
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
        public async Task<IActionResult> GetAllHeadOrdersPartial([FromBody] FilterVModel model, [FromQuery] int fStatus = -1)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var rols = await userManager.GetRolesAsync(user);

                bool hasFilter = model.filt != null;
                bool hasSearch = !string.IsNullOrEmpty(model.serVal);


                var query = db.OrderHeads.AsNoTracking()
                    .Include(f => f.RequestAmountOrderHeads)
                    .OrderByDescending(f => f.CreateDate)
                    .AsQueryable();

                if (hasSearch)
                {
                    query = query.Where(item =>
                        item.CustomerTxt.Contains(model.serVal) ||
                        item.OrderNumber.ToString() == model.serVal ||
                        item.PrefactorSer.ToString() == model.serVal ||
                        item.CustomerCode.ToString() == model.serVal
                    );
                }

                if (fStatus != -1)
                {
                    query = fStatus switch {
                        1 => query.Where(f => db.PreFactors.Any(p => p.Id == f.Prefactor.Value && p.Factors.Count > 0)),
                        2 => query.Where(f => !db.PreFactors.Any(p => p.Id == f.Prefactor.Value && p.Factors.Count > 0)),
                        _ => query
                    };
                }

                if (hasFilter)
                {
                    query = query.ApplyFilter(model, f => f.Customer, f => f.OrderDateMildi, f => f.UserCreatorID);
                }

                if (!rols.Contains("مدیر ارشد") && !rols.Contains("مدیر فروش") && !rols.Contains("کارشناس مالی") && !rols.Contains("مدیر مالی"))
                {
                    query = rols.Contains("کارشناس تامین")
                        ? query.Where(f => f.UserCreatorID == user.Id || f.UserTaminId == Guid.Parse(user.Id))
                        : query.Where(f => f.UserCreatorID == user.Id);
                }


                if (!hasFilter && !hasSearch) //اگر فیلتر نداشت پیش فرض سفارش هایی که از این تاریخ به بعد هستند رو نمایش میده
                {
                    var date = dtService.GetDateMiladi("1404/01/01");
                    query = query.Where(f => f.OrderDateMildi > date).Take(200);
                }

                // single mode
                if (model._type > 0)
                {
                    var result = await BuildOrderColumn(query, (OrderHeadStatusEnum)model._type);
                    return Ok(result);
                }

                // multi mode
                var registered = await BuildOrderColumn(query, OrderHeadStatusEnum.ثبت_اولیه);
                var producing = await BuildOrderColumn(query, OrderHeadStatusEnum.تولید);
                var finished = await BuildOrderColumn(query, OrderHeadStatusEnum.آماده_تحویل);
                var deliveredCount = 0;

                if (hasSearch || hasFilter)
                {
                    deliveredCount = await query.CountAsync(f => f.Status == OrderHeadStatusEnum.تحویل_شده);
                }

                return Ok(new { registered, producing, finished, deliveredCount });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException);
            }
        }


        public async Task<IActionResult> GetAllDealOrdersPartial(Guid dealId)
        {
            var orders = db.OrderHeads.AsNoTracking().Where(f => f.DealId == dealId);

            var registered = await BuildOrderColumn(orders, OrderHeadStatusEnum.ثبت_اولیه, 4);
            var producing = await BuildOrderColumn(orders, OrderHeadStatusEnum.تولید, 4);
            var finished = await BuildOrderColumn(orders, OrderHeadStatusEnum.آماده_تحویل, 4);
            var delivered = await BuildOrderColumn(orders, OrderHeadStatusEnum.تحویل_شده, 4);

            var model = new {
                dealOrderRegistered = registered,
                dealOrderProducing = producing,
                dealOrderFinished = finished,
                dealOrderDelivered = delivered
            };

            return Ok(model);
        }

        private async Task<object> BuildOrderColumn(IQueryable<OrderHead> query, OrderHeadStatusEnum status, int passType = 1)
        {
            var data = await query
                .Where(f => f.Status == status)
                .Include(f => f.Deal)
                .ToListAsync();

            var html = await vrs.RenderToStringAsync("GetAllHeadOrdersPartial", data,
                new Dictionary<string, object> {
                    { "PassType", passType }
                }
            );

            return new { count = data.Count, html };
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
                var setting = await db.ASettings.FirstOrDefaultAsync();

                ViewBag.MultiSelect = true;
                ViewBag.PassType = model.PassType;
                if (model.PassType == 4)
                {
                    ViewBag.MultiSelect = setting.MultiSelectOrderHeadsInRequestAmount;
                }

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

                foreach (var model in models ?? [])
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
    }
}