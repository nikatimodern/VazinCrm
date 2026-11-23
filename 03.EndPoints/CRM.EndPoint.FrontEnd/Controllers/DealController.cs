using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Deals.Enums;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.Infrastructures.Sql;
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
    private readonly IAccessScopeService accessScopes;
    private readonly IConfiguration configuration;
    private bool Enforce => configuration.GetValue<bool>("AccessCatalog:EnforceDealPermissions");
    private bool CustomerEnforce => configuration.GetValue<bool>("AccessCatalog:EnforceCustomerPermissions");
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    private Task<byte> ScopeAsync(string key, CancellationToken ct) =>
        Enforce ? accessScopes.GetEffectiveScopeAsync(CurrentUserId, "Deal." + key, ct)
                : Task.FromResult(AccessScopes.All);

    private async Task<bool> HasPermissionAsync(string key, CancellationToken ct) =>
        !Enforce || await ScopeAsync(key, ct) == AccessScopes.All;

    private Task<DealOwnerAccess> OwnerAccessAsync(string key, CancellationToken ct) =>
        Enforce ? accessScopes.GetDealOwnerAccessAsync(CurrentUserId, "Deal." + key, ct)
                : Task.FromResult(new DealOwnerAccess(null, []));

    private async Task<bool> CanAccessAsync(string key, Deal deal, CancellationToken ct) =>
        !Enforce || (await OwnerAccessAsync(key, ct)).Allows(deal.UserCreatorID);

    private static IQueryable<Deal> ApplyOwnerFilter(IQueryable<Deal> query, DealOwnerAccess filter)
    {
        if (filter.AllowedOwnerIds is { } allowed)
            query = query.Where(d => allowed.Contains(d.UserCreatorID));
        if (filter.ExcludedOwnerIds.Length > 0)
        {
            var excluded = filter.ExcludedOwnerIds;
            query = query.Where(d => !excluded.Contains(d.UserCreatorID));
        }
        return query;
    }

    public DealController(CRMContextDb db, UserManager<AppUser> userManager, DateTimeServices dtService, Service genService,
        LogSystemService logSystemService, UserContextDb udb, IAccessScopeService accessScopes,
        IConfiguration configuration)
    {
        this.db = db;
        this.userManager = userManager;
        this.dtService = dtService;
        this.genService = genService;
        _logSystemService = logSystemService;
        this.udb = udb;
        this.accessScopes = accessScopes;
        this.configuration = configuration;
    }


    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (Enforce)
        {
            if (await ScopeAsync("View", ct) == AccessScopes.None) return Forbid();
        }
        else if (!new[] { "کارشناس فروش", "مدیر ارشد", "مدیر تامین", "مدیر فروش",
                          "کارشناس تامین", "مدیر عامل" }.Any(User.IsInRole))
        {
            return Forbid();
        }

        ViewBag.CanCreateDeal = await HasPermissionAsync("Create", ct);
        ViewBag.DealPermissionsEnforced = Enforce;
        return View();
    }


    public async Task<IActionResult> ToggleFinancialStatusDeal(Guid id, DealFinancialStausEnum status, CancellationToken ct)
    {
        if (!Enum.IsDefined(status)) return BadRequest();
        if (!await HasPermissionAsync("ApproveFinancial", ct)) return Forbid();
        var deal = await db.Deals.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (deal is null) return NotFound("رکوردی یافت نشد!");
        if (!await CanAccessAsync("View", deal, ct)) return Forbid();
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user is null) return Unauthorized();

        var oldDeal = (Deal)db.Entry(deal).OriginalValues.ToObject();
        deal.FinancialStatus = status;
        deal.UserModify = user.FullName;
        deal.UserModifyID = user.Id;
        deal.ModifyDate = DateTime.Now;
        deal.ModifyDateTxt = dtService.GetDateTimePersian();

        var logAction = status == DealFinancialStausEnum.تایید_شده
            ? LogActionEnum.تایید_و_تغییر_وضعیت : LogActionEnum.رد_و_تغییر_وضعیت;
        await _logSystemService.LogChange(oldDeal, deal, logAction, user);
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> GetAllDealsDt(CancellationToken ct)
    {
        if (Enforce && await ScopeAsync("View", ct) == AccessScopes.None) return Forbid();
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
        var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
        var searchValue = Request.Form["search[value]"].FirstOrDefault();
        var sortColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
        var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault();
        var sortColumn = Request.Form[$"columns[{sortColumnIndex}][data]"].FirstOrDefault();
        var model = JsonConvert.DeserializeObject<FilterVModel>(Request.Form["model"].FirstOrDefault());

        var baseQuery = db.Deals.AsNoTracking()
            .Where(d => d.Status == DealStatusEnum.موفق);
        if (Enforce)
            baseQuery = ApplyOwnerFilter(baseQuery, await OwnerAccessAsync("View", ct));

        if (!string.IsNullOrEmpty(searchValue))
        {
            var serVal = searchValue.Trim();
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

        var recordsTotal = await query.CountAsync();

        if (length != -1)
        {
            query = query.Skip(start).Take(length);
        }

        var data = await query.ToListAsync();

        await SetDealCountsAsync(data);

        return Json(new {
            draw,
            recordsTotal,
            recordsFiltered = recordsTotal,
            data,
            filterCounts
        });
    }


    [HttpPost]
    public async Task<IActionResult> GetAllDeals([FromBody] FilterVModel model)
    {
        try
        {
            if (Enforce && await ScopeAsync("View", HttpContext.RequestAborted) == AccessScopes.None)
                return Forbid();
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user is null) return Unauthorized();
            var roles = await userManager.GetRolesAsync(user);

            var query = db.Deals.Include(f => f.Customer).AsNoTracking().AsQueryable();
            var isAdmin = Enforce
                ? await HasPermissionAsync("Assign", HttpContext.RequestAborted)
                : roles.Contains("مدیر ارشد") || roles.Contains("مدیر فروش") || roles.Contains("مدیر عامل");
            if (Enforce)
                query = ApplyOwnerFilter(query, await OwnerAccessAsync("View", HttpContext.RequestAborted));
            else if (!isAdmin)
                query = query.Where(f => f.UserCreatorID == user.Id);

            if (!string.IsNullOrWhiteSpace(model?.EntityId))
            {
                if (!Guid.TryParse(model.EntityId, out var dealId))
                    return BadRequest();

                query = query.Where(f => f.Id == dealId);
            }

            if (!string.IsNullOrWhiteSpace(model?.serVal))
            {
                var serVal = model.serVal.Trim();
                query = query.Where(f =>
                    f.SerialNumber.ToString() == serVal ||
                    f.CustomerCode.ToString() == serVal ||
                    f.Description.Contains(serVal) ||
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

            foreach (var item in deals.Where(item => item.Suspend && isAdmin))
            {
                var userList = await db.UserCustomers
                    .Where(f => f.CustomerId == item.CustomerId)
                    .Select(f => new AppUser {
                        Id = f.UserId.ToString(),
                        FirstName = users.GetValueOrDefault(f.UserId)
                    })
                    .AsNoTracking()
                    .ToListAsync();

                userList.Add(new AppUser {
                    Id = item.UserCreatorID,
                    FirstName = item.UserCreator
                });

                item.UserIdsForSuspend = userList.DistinctBy(u => Guid.Parse(u.Id)).Reverse().ToList();
            }

            var editAccess = await OwnerAccessAsync("Edit", HttpContext.RequestAborted);
            var deleteAccess = await OwnerAccessAsync("Delete", HttpContext.RequestAborted);
            var statusAccess = await OwnerAccessAsync("ChangeStatus", HttpContext.RequestAborted);
            var permissionMap = deals.ToDictionary(d => d.Id.ToString(), d => new {
                canEdit = editAccess.Allows(d.UserCreatorID) && d.FinancialStatus != DealFinancialStausEnum.تایید_شده,
                canDelete = (Enforce ? deleteAccess.Allows(d.UserCreatorID) : isAdmin) &&
                            d.FinancialStatus != DealFinancialStausEnum.تایید_شده,
                canChangeStatus = statusAccess.Allows(d.UserCreatorID),
                canAssign = isAdmin
            });

            return Ok(new {
                items = new { open, success, failed },
                isAdmin,
                canCreate = await HasPermissionAsync("Create", HttpContext.RequestAborted),
                permissionsByDeal = permissionMap
            });
        }
        catch (Exception)
        {
            return BadRequest();
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

        var supplyFactorCounts = await db.SupplyFactors
            .Where(f => f.DealId.HasValue && dealIds.Contains(f.DealId.Value))
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
            deal.Counts.SupplyFactor = supplyFactorCounts.GetValueOrDefault(deal.Id);
        }
    }

    public async Task<IActionResult> GetDeal(Guid id)
    {
        try
        {
            var deal = await db.Deals.FindAsync(id);
            if (deal == null) return NotFound("رکوردی یافت نشد!");
            if (!await CanAccessAsync("View", deal, HttpContext.RequestAborted)) return Forbid();

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
            if (model == null) return BadRequest();
            var ct = HttpContext.RequestAborted;
            var existing = model.Id == Guid.Empty ? null :
                await db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == model.Id, ct);
            if (Enforce)
            {
                if (model.Id == Guid.Empty)
                {
                    if (!await HasPermissionAsync("Create", ct)) return Forbid();
                }
                else
                {
                    if (existing is null) return NotFound();
                    if (!await CanAccessAsync("Edit", existing, ct)) return Forbid();
                    if (existing.FinancialStatus == DealFinancialStausEnum.تایید_شده)
                        return Conflict("سفارش تایید مالی‌شده قابل ویرایش نیست.");
                    if (existing.CustomerId != model.CustomerId &&
                        await db.PreFactors.AnyAsync(p => p.DealId == model.Id, ct))
                        return Conflict("مشتری سفارش دارای پیش‌فاکتور قابل تغییر نیست.");
                }
            }
            if ((Enforce || CustomerEnforce) && model.CustomerId.HasValue &&
                !await accessScopes.CanAccessCustomerAsync(CurrentUserId, "Customer.View",
                    model.CustomerId.Value, ct)) return Forbid();
            if (model.CustomerId == null && db.Customers.Any(f => f.Mobile == model.MobileNumber))
            {
                return Conflict("مشتری با این شماره موبایل قبلا در سیستم ثبت شده است!");
            }

            var user = await userManager.GetUserAsync(HttpContext.User);
            var setting = await db.ASettings.FirstOrDefaultAsync();

            if (setting.CustomerAssignExpert && model.CustomerId != null)
            {
                var dealCreatorId = model.Id == Guid.Empty ? user.Id :
                    (Enforce ? existing!.UserCreatorID : model.UserCreatorID);
                var isCustomerForUser = await db.UserCustomers.Where(f =>
                    f.CustomerId == model.CustomerId &&
                    f.UserId.ToString() == dealCreatorId
                ).AnyAsync();

                if (!isCustomerForUser)
                    model.Suspend = true;
            }


            var oldDeal = existing;
            var customer = await db.Customers.Where(f => f.Id == model.CustomerId).FirstOrDefaultAsync();
            // Deal.Create/Edit does not implicitly grant Customer.Edit.
            // Keep the linked person's fields unchanged when that grant is absent.
            var canEditCustomer = customer != null &&
                (!(Enforce || CustomerEnforce) || await accessScopes.CanAccessCustomerAsync(
                    CurrentUserId, "Customer.Edit", customer.Id, ct));
            if (canEditCustomer)
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


            if (Enforce)
            {
                // Ignore privileged fields supplied by an untrusted JSON body.
                // Status and financial approval have dedicated actions/permissions.
                model.Customer = null;
                model.Factors = null;
                model.Accountancies = null;
                model.PreFactors = null;
                model.Reports = null;
                model.OrderHeads = null;
                model.SupplyFactors = null;
                model.UserIdsForSuspend = null;
                if (existing is null)
                {
                    model.Status = DealStatusEnum.باز;
                    model.FinancialStatus = DealFinancialStausEnum.تایید_نشده;
                    model.IsDeleted = false;
                    model.Suspend = false;
                    model.OverdueFollowUpAt = null;
                    model.OverdueFollowUpNote = null;
                }
                else
                {
                    model.UserCreatorID = existing.UserCreatorID;
                    model.UserCreator = existing.UserCreator;
                    model.CreateDate = existing.CreateDate;
                    model.CreateDateTxt = existing.CreateDateTxt;
                    model.SerialNumber = existing.SerialNumber;
                    model.CountNum = existing.CountNum;
                    model.Status = existing.Status;
                    model.FinancialStatus = existing.FinancialStatus;
                    model.Suspend = existing.Suspend;
                    model.OverdueFollowUpAt = existing.OverdueFollowUpAt;
                    model.OverdueFollowUpNote = existing.OverdueFollowUpNote;
                    model.IsDeleted = existing.IsDeleted;
                }
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

                // Updating related invoices, prefactors and financial documents is
                // not an automatic side effect of Deal.Edit when Customer.Edit is denied.
                if (!Enforce || customer is null || canEditCustomer)
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
            if (deal == null) return NotFound("رکوردی یافت نشد!");
            if (!await CanAccessAsync("Delete", deal, HttpContext.RequestAborted)) return Forbid();
            if (Enforce && deal.FinancialStatus == DealFinancialStausEnum.تایید_شده)
                return Conflict("سفارش تایید مالی‌شده قابل حذف نیست.");

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
            var deal = await db.Deals.Include(f => f.Factors).Where(f => f.Id == id).FirstOrDefaultAsync();
            if (deal == null) return NotFound("رکوردی یافت نشد!");
            if (!Enum.IsDefined(status)) return BadRequest();
            if (!await CanAccessAsync("ChangeStatus", deal, HttpContext.RequestAborted)) return Forbid();
            if (Enforce && deal.FinancialStatus == DealFinancialStausEnum.تایید_شده)
                return Conflict("وضعیت سفارش تایید مالی‌شده قابل تغییر نیست.");

            if (status == DealStatusEnum.موفق && !deal.Factors.Any())
                return BadRequest("سفارش بدون فاکتور نمی‌تواند موفق شود.");

            if (deal.Factors.Any() && (status == DealStatusEnum.باز || status == DealStatusEnum.بسته || status == DealStatusEnum.ناموفق))
            {
                return BadRequest("مجاز به تغییر وضعیت این سفارش نیستید");
            } else
            {
                deal.Status = status;
                deal.UserModify = user.FullName;
                deal.UserModifyID = user.Id;
                deal.ModifyDate = DateTime.Now;
                deal.ModifyDateTxt = dtService.GetDateTimePersian();

                await _logSystemService.LogChange(oldDeal, deal, LogActionEnum.تغییر_وضعیت, user);
                await db.SaveChangesAsync();
                return Ok();
            }
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
            if (deal == null) return NotFound("رکوردی یافت نشد!");
            if (deal.CustomerId == null) return BadRequest();
            if (!await HasPermissionAsync("Assign", HttpContext.RequestAborted) ||
                !await CanAccessAsync("View", deal, HttpContext.RequestAborted)) return Forbid();
            var recipient = await udb.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId.ToString(), HttpContext.RequestAborted);
            if (Enforce && (recipient is null || !recipient.IsEmployee || recipient.OrganAdmin))
                return BadRequest("کارشناس انتخاب‌شده معتبر نیست.");
            if (Enforce && CustomerEnforce &&
                !await accessScopes.CanAccessCustomerAsync(CurrentUserId, "Customer.View",
                    deal.CustomerId.Value, HttpContext.RequestAborted)) return Forbid();

            var oldDealForLog = (Deal)db.Entry(deal).OriginalValues.ToObject();
            if (string.Equals(deal.UserCreatorID, userId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                // Assigning a customer-owner relation is a Customer.ManageExperts
                // operation as well as Deal.Assign; never smuggle it through Deal.
                if (Enforce && await accessScopes.GetEffectiveScopeAsync(CurrentUserId,
                        "Customer.ManageExperts", HttpContext.RequestAborted) != AccessScopes.All)
                    return Forbid();
                var customerId = (Guid)deal.CustomerId;
                var exists = await db.UserCustomers
                    .AnyAsync(x => x.UserId == userId && x.CustomerId == customerId);

                if (!exists)
                {
                    db.UserCustomers.Add(new UserCustomer {
                        UserId = userId,
                        CustomerId = customerId
                    });
                }
            } else
            {
                deal.UserCreatorID = userId.ToString();
                deal.UserCreator = Enforce ? ((recipient?.FirstName ?? "") + " " +
                    (recipient?.LastName ?? "")).Trim() : userFullName;
            }

            deal.Suspend = false;

            await _logSystemService.LogChange(oldDealForLog, deal, LogActionEnum.تغییر_وضعیت, user);

            await db.SaveChangesAsync();
            return Ok();
        }
        catch
        {
            return BadRequest();
        }
    }
}
