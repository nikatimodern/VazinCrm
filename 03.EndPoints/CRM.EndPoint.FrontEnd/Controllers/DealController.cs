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
using CRM.Core.Domain.Transfers.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.QueryExtensions;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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

    [Authorize(Roles = "کارشناس فروش ,مدیر ارشد ,مدیر تامین ,مدیر فروش ,کارشناس تامین, مدیر عامل")]
    public IActionResult Index()
    {
        return View();
    }


    public async Task<IActionResult> ToggleFinancialStatusDeal(Guid id, DealFinancialStausEnum status)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var deal = await db.Deals.FindAsync(id);
            if (deal == null) return NotFound();

            deal.FinancialStatus = status;

            deal.UserModify = user.FullName;
            deal.UserModifyID = user.Id;
            deal.ModifyDate = DateTime.Now;
            deal.ModifyDateTxt = dtService.GetDateTimePersian();


            var oldDeal = (Deal)db.Entry(deal).OriginalValues.ToObject();
            var logAction = status == DealFinancialStausEnum.تایید_شده ? LogActionEnum.تایید_و_تغییر_وضعیت : LogActionEnum.رد_و_تغییر_وضعیت;
            await _logSystemService.LogChange(oldDeal, deal, logAction, user);

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetAllDealsDt()
    {
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
        var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
        var searchValue = Request.Form["search[value]"].FirstOrDefault();
        var sortColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
        var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault();
        var sortColumn = Request.Form[$"columns[{sortColumnIndex}][data]"].FirstOrDefault();
        var model = JsonConvert.DeserializeObject<filterVModel>(Request.Form["model"].FirstOrDefault());

        if (string.IsNullOrEmpty(searchValue)) searchValue = model?.serVal;

        var baseQuery = db.Deals.AsNoTracking();

        var recordsTotal = await baseQuery.CountAsync();

        if (!string.IsNullOrEmpty(searchValue))
        {
            var serVal = model.serVal.Trim();
            baseQuery = baseQuery.Where(f =>
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
            baseQuery = baseQuery.ApplyFilter(model, f => f.CustomerId, f => f.CreateDate, f => f.UserCreatorID);
        }

        var filterCounts = new Dictionary<int, int> {
            [-1] = await baseQuery.CountAsync(),
            [0] = await baseQuery.CountAsync(s => s.FinancialStatus == DealFinancialStausEnum.تایید_نشده),
            [1] = await baseQuery.CountAsync(s => s.FinancialStatus == DealFinancialStausEnum.تایید_شده),
        };


        var query = model?._type switch {
            0 => baseQuery.Where(s => s.FinancialStatus == DealFinancialStausEnum.تایید_نشده),
            1 => baseQuery.Where(s => s.FinancialStatus == DealFinancialStausEnum.تایید_شده),
            _ => baseQuery
        };


        if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortDirection))
        {
            var isAsc = sortDirection == "asc";

            query = sortColumn switch {
                "serialNumber" => isAsc ? query.OrderBy(c => c.SerialNumber) : query.OrderByDescending(c => c.SerialNumber),
                "customerName" => isAsc ? query.OrderBy(c => c.CustomerName) : query.OrderByDescending(c => c.CustomerName),
                "title" => isAsc ? query.OrderBy(c => c.Title) : query.OrderByDescending(c => c.Title),
                "commentText" => isAsc ? query.OrderBy(c => c.CommentText) : query.OrderByDescending(c => c.CommentText),
                "userCreator" => isAsc ? query.OrderBy(c => c.UserCreator) : query.OrderByDescending(c => c.UserCreator),
                "status" => isAsc ? query.OrderBy(c => c.Status) : query.OrderByDescending(c => c.Status),
                _ => isAsc ? query.OrderBy(c => c.CreateDate) : query.OrderByDescending(c => c.CreateDate),
            };
        } else
        {
            query = query.OrderByDescending(c => c.CreateDate);
        }

        query = length != -1
            ? query.Skip(start).Take(length)
            : query.Take(100);

        var data = await query.ToListAsync();
        var recordsFiltered = data.Count;

        await SetDealCountsAsync(data);

        return Json(new {
            draw,
            recordsTotal,
            recordsFiltered,
            data,
            filterCounts
        });
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

            var deals = open.Concat(success).Concat(failed).ToList();
            await SetDealCountsAsync(deals);


            var users = await udb.Users.Where(f => f.IsEmployee).Select(f => new { f.Id, f.FirstName, f.LastName })
                .ToDictionaryAsync(x => Guid.Parse(x.Id), x => x.FirstName + " " + x.LastName);

            foreach (var item in deals.Where(item => item.Suspend))
            {
                var userList = await db.UserCustomers
                    .Where(f => f.CustomerId == item.CustomerId)
                    .Select(f => new AppUser {
                        Id = f.UserId.ToString(),
                        FirstName = users.GetValueOrDefault(f.UserId)
                    })
                    .ToListAsync();

                userList.Add(new AppUser {
                    Id = item.UserCreatorID,
                    FirstName = item.UserCreator
                });

                item.UserIdsForSuspend = userList.DistinctBy(u => Guid.Parse(u.Id)).Reverse().ToList();
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

    private async Task SetDealCountsAsync(IList<Deal> deals)
    {
        var dealIds = deals.Select(d => d.Id).ToList();

        if (dealIds.Count == 0)
            return;

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
            .Where(f =>
                f.DealId.HasValue &&
                dealIds.Contains(f.DealId.Value) &&
                f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور &&
                f.AccType != AcEnum.بدهکاری)
            .GroupBy(f => f.DealId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        foreach (var deal in deals)
        {
            deal.Counts.PreFactor = preFactorCounts.GetValueOrDefault(deal.Id);
            deal.Counts.Factor = factorCounts.GetValueOrDefault(deal.Id);
            deal.Counts.Report = reportCounts.GetValueOrDefault(deal.Id);
            deal.Counts.OrderHead = orderHeadCounts.GetValueOrDefault(deal.Id);
            deal.Counts.Accountancy = accCounts.GetValueOrDefault(deal.Id);
        }
    }

    public async Task<IActionResult> GetDeal(Guid id)
    {
        try
        {
            var deal = await db.Deals.FindAsync(id);
            if (deal == null) return NotFound();

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
                var dealCreatorId = model.Id == Guid.Empty ? user.Id : model.UserCreatorID;
                var isCustomerForUser = await db.UserCustomers.Where(f =>
                    f.CustomerId == model.CustomerId &&
                    f.UserId.ToString() == dealCreatorId
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
            if (deal == null) return NotFound();

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
            if (deal == null) return NotFound();

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

    public async Task<IActionResult> AssignUserSuspendDeal(Guid dealId, Guid userId, string userFullName)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            var deal = await db.Deals.FirstOrDefaultAsync(f => f.Id == dealId);
            if (deal == null) return NotFound();
            if (deal.CustomerId == null) return BadRequest();

            if (Guid.Parse(deal.UserCreatorID) == userId)
            {
                await db.UserCustomers.AddAsync(new UserCustomer {
                    UserId = userId,
                    CustomerId = (Guid)deal.CustomerId
                });
            } else
            {
                deal.UserCreatorID = userId.ToString();
                deal.UserCreator = userFullName;
            }

            deal.Suspend = false;

            var oldDeal = (Deal)db.Entry(deal).OriginalValues.ToObject();
            await _logSystemService.LogChange(oldDeal, deal, LogActionEnum.تغییر_وضعیت, user);

            await db.SaveChangesAsync();
            return Ok();
        }
        catch
        {
            return BadRequest();
        }
    }
}