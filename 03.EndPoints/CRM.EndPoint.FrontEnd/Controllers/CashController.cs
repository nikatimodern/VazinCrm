using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Cashes.Enums;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Cash;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.Factor;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class CashController : Controller {
    private readonly CRMContextDb db;
    private readonly UserContextDb udb;
    private readonly UserManager<AppUser> userManager;
    private readonly DateTimeServices dtService;
    private readonly LogSystemService logSystemService;
    private readonly FileSaver fileSaver;
    private readonly SepidarService sepidarService;

    public CashController(CRMContextDb db, UserContextDb udb, UserManager<AppUser> userManager, DateTimeServices dtService,
        LogSystemService logSystemService, FileSaver fileSaver, SepidarService sepidarService)
    {
        this.db = db;
        this.udb = udb;
        this.userManager = userManager;
        this.dtService = dtService;
        this.logSystemService = logSystemService;
        this.fileSaver = fileSaver;
        this.sepidarService = sepidarService;
    }

    [Authorize(Roles = "مدیر ارشد,تنخواه دار,مدیر مالی,کارشناس مالی")]
    public async Task<IActionResult> Index()
    {
        var isTaminUser = User.IsTamin();
        var model = new CashIndexVModel {
            IsTaminUser = isTaminUser,
            CashCenters = await GetAllCashCenters(),
            CashTitles = await GetAllCashTitles(isTaminUser)
        };
        return View(model);
    }


    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public IActionResult Cashier()
    {
        return View();
    }

    [Authorize(Roles = "مدیر ارشد,تنخواه دار,مدیر مالی,کارشناس مالی")]
    [HttpPost]
    public async Task<IActionResult> AddCash([FromBody] Cash model)
    {
        if (model == null) return BadRequest();

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            model.CostDateMiladi = dtService.GetDateMiladi(model.CostDate) ?? DateTime.Now;
            model.StatusTxt = model.Status.ToString().Replace("_", " ");

            if (model.Id == Guid.Empty)
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
                db.Cashes.Add(model);

                await db.SaveChangesAsync();
                await logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
            } else
            {
                var oldCash = await db.Cashes.Include(f => f.CashOrderHeads).FirstOrDefaultAsync(f => f.Id == model.Id);
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;

                await logSystemService.LogChange(oldCash, model, LogActionEnum.ویرایش, user, ["CashOrderHeads"]);

                db.Entry(model).Property(f => f.Serial).IsModified = false;
                db.Entry(oldCash).CurrentValues.SetValues(model);
                oldCash.CashOrderHeads = model.CashOrderHeads;
            }

            var attach = fileSaver.SaveAttachments(model.Attachments, _AttachType.Cash);
            foreach (var att in attach ?? [])
            {
                att.EntityId = model.Id;
            }

            db.Attachments.UpdateRange(attach);

            await SyncCashAccountancyDebit(model, user);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }

    [Authorize(Roles = "مدیر ارشد,مدیر مالی,کارشناس مالی")]
    [HttpPost]
    public async Task<IActionResult> AddCashier([FromBody] Cashier model)
    {
        if (model == null) return BadRequest();

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            if (model.Id == Guid.Empty)
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
                await db.Cashiers.AddAsync(model);
            } else
            {
                var data = await db.Cashiers.Where(f => f.Id == model.Id).FirstOrDefaultAsync();
                data.ModifyDate = DateTime.Now;
                data.ModifyDateTxt = dtService.GetDateTimePersian();
                data.UserModify = user.FullName;
                data.UserModifyID = user.Id;
                data.UserId = model.UserId;
                data.UserTxt = model.UserTxt;
            }

            var usr = await udb.Users.Where(f => f.Id == model.UserId.ToString()).FirstOrDefaultAsync();
            await userManager.AddToRoleAsync(usr, "تنخواه دار");


            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }

    public async Task<IActionResult> GetCash(Guid id)
    {
        try
        {
            var data = await db.Cashes.Where(f => f.Id == id)
                .AsNoTracking()
                .Include(f => f.CashOrderHeads)
                .Select(c => new {
                    Cash = c,
                    OrderHeadSerials = c.CashOrderHeads.Select(f => f.OrderHead.OrderNumber).ToList(),
                    Attachments = db.Attachments
                        .Where(a => a.EntityId == c.Id && a._AttachType == _AttachType.Cash)
                        .ToList()
                })
                .FirstOrDefaultAsync();
            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }


    private async Task ChangeCashStatus(CashStatusEnum status, Cash data, AppUser user)
    {
        data.Status = status;
        data.StatusTxt = status.ToString().Replace("_", " ");

        data.ModifyDate = DateTime.Now;
        data.ModifyDateTxt = dtService.GetDateTimePersian();
        data.UserModify = user.FullName;
        data.UserModifyID = user.Id;

        await SyncCashAccountancyDebit(data, user);
    }

    private async Task AttachCashesToNewGroup(List<Cash> cashList, CashStatusEnum status, AppUser user)
    {
        if (cashList == null || cashList.Count == 0) return;

        var group = new CashGroup {
            UserCreator = user.FullName,
            UserCreatorID = user.Id
        };

        db.CashGroups.Add(group);

        foreach (var cash in cashList)
        {
            await ChangeCashStatus(status, cash, user);
            cash.CashGroup = group;
        }
    }


    public async Task<IActionResult> ChangeStatusCash(Guid id, CashStatusEnum status)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var data = await db.Cashes.FirstOrDefaultAsync(f => f.Id == id);
            if (data == null) return NotFound("تنخواه یافت نشد.");

            if (data.CashGroupId == null && data.Status == CashStatusEnum.ثبت_موقت && status == CashStatusEnum.در_دست_تایید)
            {
                await AttachCashesToNewGroup([data], status, user);
            } else
            {
                await ChangeCashStatus(status, data, user);
            }

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> ChangeStatusAllCashToPending()
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var cashes = await db.Cashes.Where(f =>
                f.UserCreatorID == user.Id &&
                f.Status == CashStatusEnum.ثبت_موقت
            ).ToListAsync();

            if (cashes.Count == 0) return BadRequest("هیچ تنخواه آماده ارسالی یافت نشد.");

            await AttachCashesToNewGroup(cashes, CashStatusEnum.در_دست_تایید, user);

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatusGroupCashToPending([FromBody] List<Guid> listIds)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            if (listIds == null || listIds.Count == 0) return BadRequest("لیست انتخاب‌شده خالی است.");
            var user = await userManager.GetUserAsync(HttpContext.User);
            var cashes = await db.Cashes.Where(f => listIds.Contains(f.Id)).ToListAsync();

            if (cashes.Count == 0) return BadRequest("موردی برای ارسال یافت نشد.");

            await AttachCashesToNewGroup(cashes, CashStatusEnum.در_دست_تایید, user);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }

    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> ApproveCashGroup(Guid groupId)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var cashes = await db.Cashes.Where(f => f.CashGroupId == groupId).ToListAsync();

            if (cashes.Count == 0) return NotFound("گروه یافت نشد یا تنخواهی در آن وجود ندارد.");

            foreach (var cash in cashes)
            {
                await ChangeCashStatus(CashStatusEnum.تایید_شده, cash, user);
            }

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> RejectCashGroup(Guid groupId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var group = await db.CashGroups.FirstOrDefaultAsync(g => g.Id == groupId);
            var cashes = await db.Cashes.Where(f => f.CashGroupId == groupId).ToListAsync();

            if (cashes.Count == 0 && group == null) return NotFound("گروه مورد نظر یافت نشد.");

            foreach (var cash in cashes)
            {
                await ChangeCashStatus(CashStatusEnum.ثبت_موقت, cash, user);
                cash.CashGroupId = null;
            }

            if (group != null)
            {
                db.CashGroups.Remove(group);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }


    private async Task SyncCashAccountancyDebit(Cash cash, AppUser user)
    {
        if (cash == null) return;

        var acc = await db.Accountancies.FirstOrDefaultAsync(x => x.CashId == cash.Id);

        switch (cash.Status)
        {
            case CashStatusEnum.ثبت_موقت:
            {
                if (acc == null)
                {
                    acc = new Accountancy {
                        CashId = cash.Id,
                        CashierId = Guid.Parse(cash.UserCreatorID),
                        InvoiceType = InvoiceTypeEnum.تنخواه,
                        InvoiceTypeTxt = nameof(InvoiceTypeEnum.تنخواه),
                        AccType = AcEnum.بدهکاری,
                        AccTypeTxt = nameof(AcEnum.بدهکاری),
                        debit = cash.TotalAmount,
                        UserCreatorID = user?.Id,
                        UserCreator = user?.FullName,
                        PayType = PayTypeEnum.نقدی,
                        PayTypeTxt = nameof(PayTypeEnum.نقدی),
                        Status = AccStatusEnum.ثبت_اولیه,
                    };
                    await db.AddAsync(acc);
                } else
                {
                    if (acc.debit != cash.TotalAmount)
                    {
                        acc.debit = cash.TotalAmount;
                        acc.UserModifyID = user?.Id;
                        acc.UserModify = user?.FullName;
                        acc.ModifyDate = DateTime.Now;
                        acc.ModifyDateTxt = dtService.GetDateTimePersian();
                    }

                    acc.Status = AccStatusEnum.ثبت_اولیه;
                    acc.Accepted = false;
                    acc.AcceptedUser = null;
                    acc.AcceptedUserId = null;
                }

                break;
            }

            case CashStatusEnum.در_دست_تایید:
            {
                if (acc != null)
                {
                    acc.Status = AccStatusEnum.در_دست_تایید;
                    acc.Accepted = false;
                    acc.AcceptedUser = null;
                    acc.AcceptedUserId = null;
                }

                break;
            }

            case CashStatusEnum.تایید_شده:
            {
                if (acc != null)
                {
                    acc.Status = AccStatusEnum.تایید_شده;
                    acc.Accepted = true;
                    acc.AcceptedUser = user?.FullName;
                    acc.AcceptedUserId = user?.Id;
                }

                break;
            }
        }
    }

    public async Task<IActionResult> GetCashier(Guid id)
    {
        try
        {
            var data = await db.Cashiers.Where(f => f.Id == id).AsNoTracking().FirstOrDefaultAsync();
            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> DeleteCash(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.Cashes.Where(f => f.Id == id).FirstOrDefaultAsync();
            if (item == null) return NotFound("رکوردی یافت نشد!");

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;

            await logSystemService.LogChange(item, null, LogActionEnum.حذف, user);

            var acc = await db.Accountancies.FirstOrDefaultAsync(x => x.CashId == id);
            if (acc != null)
            {
                db.Accountancies.Remove(acc);
            }

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> DeleteCashier(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.Cashiers.Where(f => f.Id == id).FirstOrDefaultAsync();
            if (item == null) return NotFound("رکوردی یافت نشد!");

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;


            var usr = await udb.Users.FirstOrDefaultAsync(f => f.Id == item.UserId.ToString());
            await userManager.RemoveFromRoleAsync(usr, "تنخواه دار");


            await logSystemService.LogChange(item, null, LogActionEnum.حذف, user);

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }


    public async Task<IActionResult> GetAllCashesDt()
    {
        try
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
            var model = JsonConvert.DeserializeObject<FilterVModel>(Request.Form["model"].FirstOrDefault());
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][data]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.ToLower().Trim();

            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);

            var isExported = false;

            var query = db.Cashes.Include(f => f.CashGroup).AsQueryable();

            if (Guid.TryParse(model?.EntityId, out var groupId))
            {
                query = query.Where(f => f.CashGroupId == Guid.Parse(model.EntityId));
                isExported = await db.CashGroups
                    .Where(g => g.Id == groupId)
                    .Select(g => g.IsExported)
                    .FirstOrDefaultAsync();
            }


            if (!roles.Contains("مدیر ارشد") && !roles.Contains("مدیر مالی") && !roles.Contains("کارشناس مالی"))
            {
                query = query.Where(f => f.UserCreatorID == user.Id);
            } else
            {
                query = query.Where(f => f.Status != CashStatusEnum.ثبت_موقت || f.UserCreatorID == user.Id);
            }


            if (!string.IsNullOrEmpty(searchValue))
            {
                var searchNumber = TextService.convertToInt(searchValue);
                query = query.Where(f =>
                    f.Amount == searchNumber ||
                    f.CashGroup.GroupNumber == searchNumber ||
                    f.Serial == searchNumber ||
                    f.CostDescription.Contains(searchValue) ||
                    f.CostCenterTxt.Contains(searchValue) ||
                    f.CostTitleTxt.Contains(searchValue)
                );
            }

            if (model?.filt != null)
            {
                query = query.ApplyFilter(model, null, d => d.CreateDate, f => f.UserCreatorID);
            }

            query = model._type switch {
                1 => query.Where(f => f.Status == CashStatusEnum.در_دست_تایید),
                2 => query.Where(f => f.Status == CashStatusEnum.تایید_شده),
                _ => query
            };

            var recordsTotal = query.Count();


            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
            {
                var isAscending = sortColumnDirection == "asc";
                query = sortColumn switch {
                    "serial" => isAscending
                        ? query.OrderBy(f => f.Serial)
                        : query.OrderByDescending(f => f.Serial),
                    "createDateTxt" => isAscending
                        ? query.OrderBy(f => f.CreateDate)
                        : query.OrderByDescending(f => f.CreateDate),
                    "costTitleTxt" => isAscending
                        ? query.OrderBy(f => f.CostTitleTxt)
                        : query.OrderByDescending(f => f.CostTitleTxt),
                    "costCenterTxt" => isAscending
                        ? query.OrderBy(f => f.CostCenterTxt)
                        : query.OrderByDescending(f => f.CostCenterTxt),
                    "costDate" => isAscending
                        ? query.OrderBy(f => f.CostDateMiladi)
                        : query.OrderByDescending(f => f.CostDateMiladi),
                    "amount" => isAscending
                        ? query.OrderBy(f => f.Amount)
                        : query.OrderByDescending(f => f.Amount),
                    "fee" => isAscending
                        ? query.OrderBy(f => f.Fee)
                        : query.OrderByDescending(f => f.Fee),
                    "totalAmount" => isAscending
                        ? query.OrderBy(f => f.Amount + f.Fee)
                        : query.OrderByDescending(f => f.Amount + f.Fee),
                    "costDescription" => isAscending
                        ? query.OrderBy(f => f.CostDescription)
                        : query.OrderByDescending(f => f.CostDescription),
                    "userCreator" => isAscending
                        ? query.OrderBy(f => f.UserCreator)
                        : query.OrderByDescending(f => f.UserCreator),
                    "cashGroup.groupNumber" => isAscending
                        ? query.OrderBy(f => f.CashGroup == null).ThenBy(f => f.CashGroup.GroupNumber)
                        : query.OrderBy(f => f.CashGroup == null).ThenByDescending(f => f.CashGroup.GroupNumber),
                    "paymentVoucherNumber" => isAscending
                        ? query.OrderBy(f => f.CashGroup == null).ThenBy(f => f.PaymentVoucherNumber)
                        : query.OrderBy(f => f.CashGroup == null).ThenByDescending(f => f.PaymentVoucherNumber),
                    _ => query
                };
            }


            if (length != -1)
                query = query.Skip(start).Take(length);


            object cashierBalance = null;
            if (roles.Contains("تنخواه دار"))
            {
                var cashierId = Guid.Parse(user.Id);
                var accS = await db.Accountancies
                    .Where(x => x.CashierId == cashierId)
                    .Select(x => new { x.debit, x.credit, x.Accepted })
                    .ToListAsync();

                cashierBalance = new {
                    CashierId = cashierId,
                    TotalCredit = accS.Sum(a => a.credit),
                    TotalDebit = accS.Sum(a => a.debit),
                    isAccepted = accS.All(a => a.Accepted == true)
                };
            }


            var data = await query.ToListAsync();

            await AppendOrderHeadNumbersToCashDescriptions(data);

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data,
                isExported,
                cashierBalance
            });
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    private async Task AppendOrderHeadNumbersToCashDescriptions(List<Cash> cashes)
    {
        var cashIds = cashes.Select(x => x.Id).ToList();

        var orderNumbers = await db.CashOrderHeads.AsNoTracking()
            .Where(x => cashIds.Contains(x.CashId))
            .Select(x => new {
                x.CashId,
                x.OrderHead.OrderNumber
            })
            .ToListAsync();

        var orderNumbersLookup = orderNumbers.ToLookup(x => x.CashId, x => x.OrderNumber);

        foreach (var cash in cashes)
        {
            var numbers = orderNumbersLookup[cash.Id].ToList();

            if (numbers.Count > 0)
            {
                cash.CostDescription += Environment.NewLine + $"(حواله تولید: {string.Join("، ", numbers)})";
            }
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetCashGroupsDt()
    {
        try
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
//            var model = JsonConvert.DeserializeObject<filterVModel>(Request.Form["model"].FirstOrDefault());
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][data]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.ToLower().Trim();

            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);

            var baseQuery = db.CashGroups.AsQueryable();

            if (!roles.Contains("مدیر ارشد") && !roles.Contains("مدیر مالی") && !roles.Contains("کارشناس مالی"))
            {
                baseQuery = baseQuery.Where(g => g.UserCreatorID == user.Id || g.Cashes.Any(c => c.UserCreatorID == user.Id));
            } else
            {
                baseQuery = baseQuery.Where(g =>
                    g.Cashes.Any(c => c.Status != CashStatusEnum.ثبت_موقت) || g.Cashes.Any(c => c.UserCreatorID == user.Id));
            }

            var query = baseQuery.Select(g => new CashGroupVModel {
                CashGroupId = g.Id,
                GroupNumber = g.GroupNumber,
                UserCreator = g.UserCreator,
                CreateDate = g.CreateDate,
                CreateDateTxt = g.CreateDateTxt,
                ItemsCount = g.Cashes.Count,
                TotalAmount = g.Cashes.Sum(c => c.Amount + c.Fee),
                Status = g.Cashes.All(c => c.Status == CashStatusEnum.تایید_شده) ? CashStatusEnum.تایید_شده : CashStatusEnum.در_دست_تایید,
                StatusTxt = g.Cashes.All(c => c.Status == CashStatusEnum.تایید_شده) ? "تایید شده" : "در دست تایید"
            });


            if (!string.IsNullOrEmpty(searchValue))
            {
                var searchNumber = TextService.convertToInt(searchValue);
                query = query.Where(f =>
                    f.GroupNumber == searchNumber ||
                    f.UserCreator.Contains(searchValue)
                );
            }

            var recordsTotal = await query.CountAsync();

            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
            {
                var isAscending = sortColumnDirection == "asc";
                query = sortColumn switch {
                    "groupNumber" => isAscending
                        ? query.OrderBy(f => f.GroupNumber)
                        : query.OrderByDescending(f => f.GroupNumber),
                    "createDateTxt" => isAscending
                        ? query.OrderBy(f => f.CreateDate)
                        : query.OrderByDescending(f => f.CreateDate),
                    "userCreator" => isAscending
                        ? query.OrderBy(f => f.UserCreator)
                        : query.OrderByDescending(f => f.UserCreator),
                    "itemsCount" => isAscending
                        ? query.OrderBy(f => f.ItemsCount)
                        : query.OrderByDescending(f => f.ItemsCount),
                    "totalAmount" => isAscending
                        ? query.OrderBy(f => f.TotalAmount)
                        : query.OrderByDescending(f => f.TotalAmount),
                    _ => query
                };
            } else
            {
                query = query.OrderByDescending(x => x.CreateDate);
            }

            if (length != -1)
                query = query.Skip(start).Take(length);

            var data = await query.ToListAsync();

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data
            });
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }


    [HttpGet]
    public async Task<IActionResult> ExportCashGroupToExcel(Guid groupId, int? startVoucherNumber = null)
    {
        try
        {
            var cashItems = await db.Cashes
                .Include(c => c.CashGroup).Include(c => c.CostCenter)
                .Include(c => c.CostTitle).Include(c => c.BankAccount)
                .Where(c => c.CashGroupId == groupId)
                .OrderBy(c => c.CreateDate)
                .ToListAsync();

            if (cashItems.Count == 0)
                return NotFound("اقلامی برای این گروه یافت نشد.");

            if (startVoucherNumber.HasValue)
            {
                if (cashItems[0]?.CashGroup?.IsExported == true) return BadRequest("این گروه قبلاً خروجی گرفته است.");

                var usedNumbers = await GetUsedVoucherNumbers(startVoucherNumber.Value, cashItems.Count);

                if (usedNumbers.Any())
                    return BadRequest($"شماره اعلامیه‌های {string.Join("، ", usedNumbers)} قبلاً ثبت شده‌اند. لطفاً شماره شروع دیگری وارد کنید.");


                await AssignPaymentVoucherNumbers(cashItems, startVoucherNumber.Value);
            }

            var columns = new List<ExcelColumnConfig<Cash>> {
                new() { Header = "نوع قلم", ValueSelector = _ => "PaymentDraft" },
                new() { Header = "اعلاميه پرداخت نوع پرداخت", ValueSelector = _ => 2 },
                new() { Header = "اعلاميه پرداخت طرف مقابل", ValueSelector = x => x.CostCenter?.Title },
                new() { Header = "اعلاميه پرداخت كد تفصيل", ValueSelector = x => x.CostCenter?.Code },
                new() { Header = "اعلاميه پرداخت شماره", ValueSelector = x => x.PaymentVoucherNumber },
                new() { Header = "اعلاميه پرداخت تاريخ", ValueSelector = x => x.CostDate },
                new() { Header = "اعلاميه پرداخت كد معين", ValueSelector = x => x.CostTitle?.MoeinCode },
                new() { Header = "اعلاميه پرداخت صندوق", ValueSelector = _ => "صندوق" },
                new() { Header = "اعلاميه پرداخت مبلغ پرداخت", ValueSelector = x => x.Amount }, // TODO
                new() { Header = "اعلاميه پرداخت مبلغ نقد", ValueSelector = _ => 0 },
                new() { Header = "اعلاميه پرداخت شرح", ValueSelector = x => x.CostDescription },
                new() { Header = "اعلاميه پرداخت شرح(2)", ValueSelector = x => x.CostDescription },
                new() { Header = "پرداخت چك حساب بانكي1", ValueSelector = _ => "" },
                new() { Header = "پرداخت چك شماره", ValueSelector = _ => "" },
                new() { Header = "پرداخت چك پشت نمره", ValueSelector = _ => "" },
                new() { Header = "پرداخت چك مبلغ", ValueSelector = _ => "" },
                new() { Header = "پرداخت چك تاريخ سررسيد", ValueSelector = _ => "" },
                new() { Header = "پرداخت چك نوع مدت", ValueSelector = _ => "" },
                new() { Header = "پرداخت چك شرح", ValueSelector = _ => "" },
                new() { Header = "پرداخت چك شرح(2)", ValueSelector = _ => "" },
                new() { Header = "پرداخت چك وضعيت", ValueSelector = _ => "" },
                new() { Header = "اعلاميه برداشت شماره", ValueSelector = x => string.IsNullOrEmpty(x.ReferenceNumber) ? "0" : x.ReferenceNumber },
                new() { Header = "اعلاميه برداشت تاريخ", ValueSelector = x => x.CostDate },
                new() { Header = "اعلاميه برداشت مبلغ", ValueSelector = x => x.Amount },
                new() { Header = "اعلاميه برداشت حساب بانكي", ValueSelector = x => x.BankAccountTxt },
                new() { Header = "اعلاميه برداشت شرح", ValueSelector = x => x.CostDescription },
                new() { Header = "اعلاميه برداشت شرح(2)", ValueSelector = _ => "" },
                new() { Header = "اعلاميه برداشت تفصيل حساب بانكي", ValueSelector = x => x.BankAccount?.AccountCode },
                new() { Header = "اعلاميه برداشت نرخ كارمزد بانكي", ValueSelector = _ => 1 },
                new() { Header = "اعلاميه برداشت كارمزد بانكي", ValueSelector = x => x.Fee == 0 ? "" : x.Fee },
                new() { Header = "اعلاميه برداشت كارمزد بانكي به ارز پايه", ValueSelector = x => x.Fee == 0 ? "" : x.Fee },
                new() { Header = "اعلاميه پرداخت كد معين كارمزد بانكي", ValueSelector = x => x.Fee == 0 ? "" : 7704 },
                new() { Header = "اعلاميه پرداخت تخفيف", ValueSelector = _ => 0 },
                new() { Header = "اعلاميه پرداخت استقراري", ValueSelector = _ => "False" }
            };

            return ExcelExportService.ExportToExcel(cashItems, columns, "تنخواه", rightToLeft: false, includeRowNumber: false);
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    private async Task AssignPaymentVoucherNumbers(List<Cash> cashItems, int startVoucherNumber)
    {
        if (cashItems?.Count == 0) return;

        var voucher = startVoucherNumber;
        var group = cashItems[0].CashGroup;

        foreach (var cash in cashItems)
            cash.PaymentVoucherNumber = voucher++;

        if (group != null)
        {
            group.IsExported = true;
            group.ModifyDate = DateTime.Now;
            group.ModifyDateTxt = dtService.GetDateTimePersian();
        }

        await db.SaveChangesAsync();
    }

    private async Task<List<int>> GetUsedVoucherNumbers(int startNumber, int count)
    {
        var endNumber = startNumber + count - 1;

        return await db.Cashes
            .Where(x =>
                x.PaymentVoucherNumber != null &&
                x.PaymentVoucherNumber >= startNumber &&
                x.PaymentVoucherNumber <= endNumber)
            .Select(x => x.PaymentVoucherNumber!.Value)
            .OrderBy(x => x)
            .ToListAsync();
    }


    public async Task<IActionResult> GetAllCashiersDt()
    {
        try
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
            // var model = JsonConvert.DeserializeObject<filterVModel>(Request.Form["model"].FirstOrDefault());
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][data]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.ToLower().Trim();


            var query = db.Cashiers.AsNoTracking()
                .GroupJoin(db.Accountancies.AsNoTracking(),
                    c => c.UserId,
                    ac => ac.CashierId,
                    (c, ac) => new CashierVModel {
                        Cashier = c,
                        TotalCredit = ac.Sum(x => x.credit ?? 0),
                        TotalDebit = ac.Sum(x => x.debit ?? 0),
                        Balance = ac.Sum(x => (x.credit ?? 0) - (x.debit ?? 0)),
                        IsAccepted = ac.All(x => x.Accepted == true)
                    }
                );

            var recordsTotal = await query.CountAsync();

            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(x =>
                    x.Cashier.UserTxt.Contains(searchValue)
                );
            }

            var recordsFiltered = await query.CountAsync();

            if (!string.IsNullOrEmpty(sortColumn))
            {
                var asc = sortColumnDirection == "asc";
                query = sortColumn switch {
                    "createDateTxt" => asc ? query.OrderBy(x => x.Cashier.CreateDate) : query.OrderByDescending(x => x.Cashier.CreateDate),
                    "cashierTxt" => asc ? query.OrderBy(x => x.Cashier.UserTxt) : query.OrderByDescending(x => x.Cashier.UserTxt),

                    _ => query.OrderByDescending(x => x.Cashier.CreateDate)
                };
            }

            if (length != -1)
                query = query.Skip(start).Take(length);

            var data = await query.ToListAsync();


            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered,
                data,
            });
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }


    public async Task<IActionResult> GetAllCashiers()
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var query = db.Cashiers.AsNoTracking();


            var data = await query.Select(f => new {
                value = f.UserId,
                text = f.UserTxt,
                target = f.UserId == Guid.Parse(user.Id)
            }).ToListAsync();

            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }


    //    BaseCashCenter
    private async Task<List<SelectOptionDto<Guid>>> GetAllCashCenters()
    {
        var query = db.BaseCenterCashes.AsNoTracking();

        var data = await query.Select(f => new SelectOptionDto<Guid> {
            Value = f.Id,
            Text = f.Title
        }).ToListAsync();

        return data;
    }

    //    BaseCash
    private async Task<List<SelectOptionDto<Guid>>> GetAllCashTitles(bool isTaminUser)
    {
        var query = db.BaseCashes
            .Where(f => isTaminUser
                ? f.BaseCashType == BaseCashTypeEnum.تامین || f.BaseCashType == BaseCashTypeEnum.عمومی
                : f.BaseCashType == BaseCashTypeEnum.عمومی
            ).AsNoTracking();

        var data = await query.Select(f => new SelectOptionDto<Guid> {
            Value = f.Id,
            Text = f.CostTitle
        }).ToListAsync();

        return data;
    }

    //    BaseCashCenter
    public async Task<IActionResult> GetAllBaseCashCenter()
    {
        try
        {
            var data = await GetAllCashCenters();
            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    //    BaseCash
    public async Task<IActionResult> GetAllBaseCashTitle()
    {
        try
        {
            var isTaminUser = User.IsTamin();
            var data = await GetAllCashTitles(isTaminUser);
            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> GetBaseCash(Guid id)
    {
        try
        {
            var data = await db.BaseCashes.FindAsync(id);
            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> GetAllBaseCashesDt()
    {
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
        var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
        var searchValue = Request.Form["search[value]"].FirstOrDefault();
        var sortColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
        var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault();
        var sortColumn = Request.Form[$"columns[{sortColumnIndex}][data]"].FirstOrDefault();

        var query = db.BaseCashes.AsNoTracking();

        var recordsTotal = await query.CountAsync();

        if (!string.IsNullOrEmpty(searchValue))
        {
            query = query.Where(t =>
                t.MoeinCode.ToString().Contains(searchValue) ||
                t.CostTitle.Contains(searchValue) ||
                t.AccountTitle.Contains(searchValue) ||
                t.CostItem.Contains(searchValue)
            );
        }

        var recordsFiltered = await query.CountAsync();

        if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortDirection))
        {
            var isAsc = sortDirection == "asc";

            query = sortColumn switch {
                "moeinCode" => isAsc ? query.OrderBy(c => c.MoeinCode) : query.OrderByDescending(c => c.MoeinCode),
                "costTitle" => isAsc ? query.OrderBy(c => c.CostTitle) : query.OrderByDescending(c => c.CostTitle),
                "accountTitle" => isAsc ? query.OrderBy(c => c.AccountTitle) : query.OrderByDescending(c => c.AccountTitle),
                _ => query.OrderByDescending(c => c.CreateDate)
            };
        } else
        {
            query = query.OrderByDescending(c => c.CreateDate);
        }


        if (length != -1)
            query = query.Skip(start).Take(length);

        var data = await query.ToListAsync();

        return Json(new {
            draw,
            recordsTotal,
            recordsFiltered,
            data,
        });
    }

    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    [HttpPost]
    public async Task<IActionResult> AddBaseCash([FromBody] BaseCash model)
    {
        if (model == null) return BadRequest();

        var isDuplicate = await db.BaseCashes.AnyAsync(x =>
            x.Id != model.Id &&
            (x.MoeinCode == model.MoeinCode || x.CostTitle.Trim() == model.CostTitle.Trim())
        );

        if (isDuplicate) return BadRequest("این عنوان هزینه قبلاً ثبت شده است.");


        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            model.BaseCashTypeTxt = model.BaseCashType.ToString();

            if (model.Id == Guid.Empty)
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;

                db.BaseCashes.Add(model);

                await logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
            } else
            {
                var oldBaseCash = await db.BaseCashes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id);

                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;

                db.BaseCashes.Update(model);

                await logSystemService.LogChange(oldBaseCash, model, LogActionEnum.ویرایش, user);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }

    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> DeleteBaseCash(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.BaseCashes.Where(f => f.Id == id).FirstOrDefaultAsync();
            if (item == null) return NotFound("رکوردی یافت نشد!");

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;

            await logSystemService.LogChange(item, null, LogActionEnum.حذف, user);

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }


    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> SendCashToSepidar(Guid id)
    {
        try
        {
            var item = await db.Cashes.Include(f => f.CostCenter).Include(f => f.CostTitle).FirstOrDefaultAsync(f => f.Id == id);
            await sepidarService.AddPaymentHeaderAsync(item);


            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}