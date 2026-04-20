using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Deals.Enums;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.QueryExtensions;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Policy = "NotCustomerOnly")]
public class DealController : Controller {
    private readonly CRMContextDb db;
    private readonly UserManager<AppUser> userManager;
    private readonly DateTimeServices dtService;
    private readonly Service genService;
    private readonly LogSystemService _logSystemService;
    private readonly UserContextDb udb;

    public DealController(CRMContextDb db, UserManager<AppUser> userManager, DateTimeServices dtService, Service genService,
        LogSystemService logSystemService, UserContextDb udb)
    {
        this.db = db;
        this.userManager = userManager;
        this.dtService = dtService;
        this.genService = genService;
        _logSystemService = logSystemService;
        this.udb = udb;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GetAllDeals([FromBody] filterVModel model)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);

            var query = db.Deals.Include(f => f.Customer).AsQueryable();
            var isAdmin = true;

            if (!roles.Contains("مدیر ارشد") && !roles.Contains("مدیر فروش"))
            {
                query = query.Where(f => f.UserCreatorID == user.Id);
                isAdmin = false;
            }

            if (!string.IsNullOrWhiteSpace(model?.serVal))
            {
                var serVal = model.serVal.Trim();
                query = query.Where(f =>
                    f.SerialNumber.ToString() == serVal ||
                    f.CustomerCode.ToString() == serVal ||
                    f.CommentText.Contains(serVal) ||
                    f.CustomerName.Contains(serVal) ||
                    f.CusCompanyName.Contains(serVal) ||
                    f.Title.Contains(serVal)
                );
            }

            if (model?.filt != null)
            {
                query = query.ApplyFilter(model, f => f.CustomerId, f => f.CreateDate, f => f.UserCreatorID);
            }

            query = query.OrderByDescending(f => f.ModifyDate);

            var openQ = query.Where(d => d.Status == DealStatusEnum.باز);
            var successQ = query.Where(d => d.Status == DealStatusEnum.موفق);
            var failedQ = query.Where(d => d.Status == DealStatusEnum.ناموفق);

            if (model?.filt == null)
            {
                openQ = openQ.Take(50);
                successQ = successQ.Take(50);
                failedQ = failedQ.Take(50);
            }

            var open = await openQ.ToListAsync();
            var success = await successQ.ToListAsync();
            var failed = await failedQ.ToListAsync();
//            var dealIds = query.Select(f => f.Id).ToArray();

            var dealIds = open.Concat(success).Concat(failed)
                .Select(d => d.Id)
                .ToList();

            var preFactorCounts = await db.PreFactors
                .Where(f => f.DealId.HasValue && dealIds.Contains(f.DealId.Value))
                .GroupBy(f => f.DealId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var factorCounts = await db.Factors
                .Where(f => f.DealId.HasValue && dealIds.Contains(f.DealId.Value))
                .GroupBy(f => f.DealId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var reportCounts = await db.Reports
                .Where(f => f.DealId.HasValue && dealIds.Contains(f.DealId.Value))
                .GroupBy(f => f.DealId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var orderHeadCounts = await db.OrderHeads
                .Where(f => f.DealId.HasValue && dealIds.Contains(f.DealId.Value))
                .GroupBy(f => f.DealId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var accCounts = await db.Accountancies
                .Where(f => f.DealId.HasValue && dealIds.Contains(f.DealId.Value) &&
                            f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور && f.AccType != AcEnum.بدهکاری)
                .GroupBy(f => f.DealId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);


            var users = await udb.Users.Where(f => f.IsEmployee).Select(f => new { f.Id, f.FirstName, f.LastName })
                .ToDictionaryAsync(x => Guid.Parse(x.Id), x => x.FirstName + " " + x.LastName);


            foreach (var item in open)
            {
                item.Counts.PreFactor = preFactorCounts.GetValueOrDefault(item.Id);
                item.Counts.Factor = factorCounts.GetValueOrDefault(item.Id);
                item.Counts.Report = reportCounts.GetValueOrDefault(item.Id);
                item.Counts.OrderHead = orderHeadCounts.GetValueOrDefault(item.Id);
                item.Counts.Accountancy = accCounts.GetValueOrDefault(item.Id);
                if (item.Suspend)
                {
                    item.UserIdsForSuspend = await db.UserCustomers
                        .Where(f => f.CustomerId == item.CustomerId)
                        .Select(f => new AppUser {
                            Id = f.UserId.ToString(),
                            FirstName = users.GetValueOrDefault(f.UserId)
                        }).ToListAsync();
                }
            }

            foreach (var item in success)
            {
                item.Counts.PreFactor = preFactorCounts.GetValueOrDefault(item.Id);
                item.Counts.Factor = factorCounts.GetValueOrDefault(item.Id);
                item.Counts.Report = reportCounts.GetValueOrDefault(item.Id);
                item.Counts.OrderHead = orderHeadCounts.GetValueOrDefault(item.Id);
                item.Counts.Accountancy = accCounts.GetValueOrDefault(item.Id);
            }

            foreach (var item in failed)
            {
                item.Counts.PreFactor = preFactorCounts.GetValueOrDefault(item.Id);
                item.Counts.Factor = factorCounts.GetValueOrDefault(item.Id);
                item.Counts.Report = reportCounts.GetValueOrDefault(item.Id);
                item.Counts.OrderHead = orderHeadCounts.GetValueOrDefault(item.Id);
                item.Counts.Accountancy = accCounts.GetValueOrDefault(item.Id);
            }

            return Ok(new {
                items = new { open, success, failed },
                isAdmin
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public async Task<IActionResult> GetDeal(Guid id)
    {
        try
        {
            var deal = await db.Deals.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            return Ok(deal);
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddDeal([FromBody] Deal model)
    {
        try
        {
            if (model.CustomerId == null && db.Customers.Any(f => f.Mobile == model.MobileNumber))
            {
                return StatusCode(201, "مشتری با این شماره موبایل قبلا در سیستم ثبت شده است!");
            }

            var user = await userManager.GetUserAsync(HttpContext.User);
            var setting = await db.ASettings.FirstOrDefaultAsync();
            if (setting.CustomerAssignExpert && model.CustomerId != null)
            {
                var isCustomerForUser = await db.UserCustomers.Where(f =>
                    f.CustomerId == model.CustomerId &&
                    f.UserId.ToString() == user.Id
                ).AnyAsync();

                if (!isCustomerForUser)
                    model.Suspend = true;
            }

            var oldDeal = await db.Deals.AsNoTracking().Where(f => f.Id == model.Id).FirstOrDefaultAsync();
            var customer = await db.Customers.Where(f => f.Id == model.CustomerId).FirstOrDefaultAsync();
            if (customer != null)
            {
                customer.NumberRegister = model.CusNumberRegister;
                customer.EghtesadiCode = model.CusEghtesadiCode;
                customer.ShenaseMeli = model.CusShenaseMeli;
                customer.AgentMeliCode = model.CusMelliCode;
                customer.PostalCode = model.CusPostalCode;
                customer.Juridical = model.CusJuridical;
                customer.Address = model.CusAddress;
                customer.Tel = model.CusTel;
                customer.Sex = model.CusSex;
                customer.Province = model.CusProvince;
                customer.City = model.CusCity;
                customer.WorkSenf = model.CusWorkSenf;
                customer.WorkSenfTxt = model.CusWorkSenfTxt;
                customer.WorkSenfDescription = model.CusWorkSenfDescription;
                customer.AcquaintanceWay = model.CusAcquaintanceWay;
                customer.AcquaintanceWayTxt = model.CusAcquaintanceWayTxt;
                customer.Mobile = model.MobileNumber;
                customer.CompanyName = model.CusCompanyName;
                customer.NormalizedCompanyName = TextService.NormalizeText(model.CusCompanyName);
                //customer.AgentName = model.CustomerName;
                //customer.NormalizedAgentName = TextService.NormalizeText(model.CustomerName);
            }


            if (model.Id == Guid.Empty)
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;

                db.Deals.Add(model);
                await db.SaveChangesAsync();
                await db.Entry(model).ReloadAsync();

                await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
            } else
            {
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();

                await genService.UpdateChildEntityDeal(model.Id, customer, model.CustomerName, model.MobileNumber);

                db.Deals.Update(model);

                await _logSystemService.LogChange(oldDeal, model, LogActionEnum.ویرایش, user);
            }

            await db.SaveChangesAsync();

            return Ok();
        }
        catch
        {
            return BadRequest();
        }
    }


    public async Task<IActionResult> DeleteDeal(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var deal = await db.Deals.Where(f => f.Id == id).FirstOrDefaultAsync();
            deal.IsDeleted = true;
            deal.UserModify = user.FullName;
            deal.UserModifyID = user.Id;
            deal.ModifyDate = DateTime.Now;
            deal.ModifyDateTxt = dtService.GetDateTimePersian();

            if (deal.CustomerId == null)
            {
                var pfs = await db.PreFactors.Where(f => f.DealId == id).ToListAsync();
                foreach (var pf in pfs)
                {
                    pf.IsDeleted = true;
                }
            }

            await _logSystemService.LogChange(deal, null, LogActionEnum.حذف, user);
            await db.SaveChangesAsync();

            return Ok();
        }
        catch
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> ChangeStatusDeal(Guid id, DealStatusEnum status)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var oldDeal = await db.Deals.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            var deal = await db.Deals.Where(f => f.Id == id).FirstOrDefaultAsync();
            deal.Status = status;
            deal.UserModify = user.FullName;
            deal.UserModifyID = user.Id;
            deal.ModifyDate = DateTime.Now;
            deal.ModifyDateTxt = dtService.GetDateTimePersian();

            await _logSystemService.LogChange(oldDeal, deal, LogActionEnum.تغییر_وضعیت, user);
            await db.SaveChangesAsync();
            return Ok();
        }
        catch
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> AssignCustomerToExpert(Guid dealId, Guid customerId, Guid? userId)
    {
        try
        {
            var deal = await db.Deals.FirstOrDefaultAsync(f => f.Id == dealId);
            if (deal == null) return NotFound();

            if (userId != null && customerId != Guid.Empty)
            {
                await db.UserCustomers.AddAsync(new UserCustomer {
                    CustomerId = customerId,
                    UserId = (Guid)userId
                });
            }

            deal.Suspend = false;
            await db.SaveChangesAsync();
            return Ok();
        }
        catch
        {
            return BadRequest();
        }
    }
}