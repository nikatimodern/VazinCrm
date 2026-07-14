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
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Cash;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Supply;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.QueryExtensions;
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
    private readonly Service genService;
    private readonly LogSystemService logSystemService;
    private readonly FileSaver fileSaver;

    public CashController(CRMContextDb db, UserContextDb udb, UserManager<AppUser> userManager, DateTimeServices dtService, Service genService,
        LogSystemService logSystemService, FileSaver fileSaver)
    {
        this.db = db;
        this.udb = udb;
        this.userManager = userManager;
        this.dtService = dtService;
        this.genService = genService;
        this.logSystemService = logSystemService;
        this.fileSaver = fileSaver;
    }

    [Authorize(Roles = "مدیر ارشد,تنخواه دار,مدیر مالی,کارشناس مالی")]
    public IActionResult Index()
    {
        return View();
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
            var isNewCash = model.Id == Guid.Empty;
            Cash oldCash = null;

            if (isNewCash)
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
            } else
            {
                oldCash = await db.Cashes.Where(f => f.Id == model.Id).AsNoTracking().FirstOrDefaultAsync();
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;
            }

            model.CostDateMiladi = dtService.GetDateMiladi(model.CostDate) ?? DateTime.Now;
            model.StatusTxt = model.Status.ToString().Replace("_", " ");

            db.Cashes.Update(model);
            if (!isNewCash)
                db.Entry(model).Property(f => f.Serial).IsModified = false;

            await db.SaveChangesAsync();

            if (isNewCash)
            {
                await logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
            } else
            {
                await logSystemService.LogChange(oldCash, model, LogActionEnum.ویرایش, user);
            }

            var attach = fileSaver.SaveAttachments(model.Attachments, _AttachType.Cash);
            foreach (var att in attach)
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
            return BadRequest(e.Message);
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
            return BadRequest(e.Message);
        }
    }

    public async Task<IActionResult> GetCash(Guid id)
    {
        try
        {
            var data = await db.Cashes.Where(f => f.Id == id).AsNoTracking()
                .Select(c => new {
                    Cash = c,
                    Attachments = db.Attachments
                        .Where(a => a.EntityId == c.Id && a._AttachType == _AttachType.Cash)
                        .ToList()
                })
                .FirstOrDefaultAsync();
            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    public async Task<IActionResult> ChangeStatusCash(Guid id, CashStatusEnum status)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var data = await db.Cashes.FirstOrDefaultAsync(f => f.Id == id);
            if (data == null) return NotFound();

            await ChangeCashStatus(status, data, user);

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
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


    public async Task<IActionResult> ChangeStatusAllCashToPending()
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var cashes = await db.Cashes.Where(f =>
                f.UserCreatorID == user.Id &&
                f.Status == CashStatusEnum.ثبت_موقت
            ).ToListAsync();

            foreach (var cash in cashes)
            {
                await ChangeCashStatus(CashStatusEnum.در_دست_تایید, cash, user);
            }

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatusGroupCashToPending([FromBody] List<Guid> listIds)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var cashes = await db.Cashes.Where(f =>
                listIds.Contains(f.Id) &&
                f.UserCreatorID == user.Id &&
                f.Status == CashStatusEnum.ثبت_موقت
            ).ToListAsync();

            if (cashes.Count == 0) return BadRequest("موردی برای ارسال یافت نشد.");

            var group = new CashGroup();
            db.CashGroups?.Add(group);

            foreach (var cash in cashes)
            {
                await ChangeCashStatus(CashStatusEnum.در_دست_تایید, cash, user);
                cash.CashGroup = group;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return BadRequest(e.Message);
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
            return BadRequest(e.Message);
        }
    }

    public async Task<IActionResult> DeleteCash(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.Cashes.Where(f => f.Id == id).FirstOrDefaultAsync();
            if (item == null) return NotFound();

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
            return BadRequest(e.Message);
        }
    }

    public async Task<IActionResult> DeleteCashier(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.Cashiers.Where(f => f.Id == id).FirstOrDefaultAsync();
            if (item == null) return NotFound();

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
            return BadRequest(e.Message);
        }
    }


    public async Task<IActionResult> GetAllCashesDt()
    {
        try
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
            var model = JsonConvert.DeserializeObject<filterVModel>(Request.Form["model"].FirstOrDefault());
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.ToLower().Trim();

            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);


            var query = db.Cashes.AsQueryable();

            var isManager = true;
            if (!roles.Contains("مدیر ارشد") && !roles.Contains("مدیر مالی") && !roles.Contains("کارشناس مالی"))
            {
                query = query.Where(f => f.UserCreatorID == user.Id);
                isManager = false;
            } else
            {
                query = query.Where(f => f.Status != CashStatusEnum.ثبت_موقت || f.UserCreatorID == user.Id);
            }


            if (!string.IsNullOrEmpty(searchValue))
            {
                var searchNumber = TextService.convertToInt(searchValue);
                query = query.Where(f =>
                    f.Amount == searchNumber ||
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
                    "Serial" => isAscending
                        ? query.OrderBy(f => f.Serial)
                        : query.OrderByDescending(f => f.Serial),
                    "CreateDateTxt" => isAscending
                        ? query.OrderBy(f => f.CreateDate)
                        : query.OrderByDescending(f => f.CreateDate),
                    "CostTitleTxt" => isAscending
                        ? query.OrderBy(f => f.CostTitleTxt)
                        : query.OrderByDescending(f => f.CostTitleTxt),
                    "CostCenterTxt" => isAscending
                        ? query.OrderBy(f => f.CostCenterTxt)
                        : query.OrderByDescending(f => f.CostCenterTxt),
                    "CostDate" => isAscending
                        ? query.OrderBy(f => f.CostDateMiladi)
                        : query.OrderByDescending(f => f.CostDateMiladi),
                    "Amount" => isAscending
                        ? query.OrderBy(f => f.Amount)
                        : query.OrderByDescending(f => f.Amount),
                    "Fee" => isAscending
                        ? query.OrderBy(f => f.Fee)
                        : query.OrderByDescending(f => f.Fee),
                    "TotalAmount" => isAscending
                        ? query.OrderBy(f => f.Amount + f.Fee)
                        : query.OrderByDescending(f => f.Amount + f.Fee),
                    "CostDescription" => isAscending
                        ? query.OrderBy(f => f.CostDescription)
                        : query.OrderByDescending(f => f.CostDescription),
                    "UserCreator" => isAscending
                        ? query.OrderBy(f => f.UserCreator)
                        : query.OrderByDescending(f => f.UserCreator),
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

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data,
                isManager,
                cashierBalance
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
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
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
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
            return BadRequest(e.Message);
        }
    }

    //    BaseCashCenter
    public async Task<IActionResult> GetAllBaseCashCenter()
    {
        try
        {
            var query = db.BaseCenterCashes.AsNoTracking();


            var data = await query.Select(f => new SelectOptionDto<Guid> {
                Value = f.Id,
                Text = f.Title
            }).ToListAsync();

            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    //    BaseCash
    public async Task<IActionResult> GetAllBaseCashTitle()
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);

            var targetType = roles.Contains("مدیر تامین") || roles.Contains("کارشناس تامین")
                ? BaseCashTypeEnum.تامین
                : BaseCashTypeEnum.عمومی;

            var query = db.BaseCashes
                .Where(f => f.BaseCashType == targetType || f.BaseCashType == BaseCashTypeEnum.هردو)
                .AsNoTracking();

            var data = await query.Select(f => new SelectOptionDto<Guid> {
                Value = f.Id,
                Text = f.CostTitle
            }).ToListAsync();

            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
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
            return BadRequest(e.Message);
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
            return BadRequest(e.Message);
        }
    }

    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> DeleteBaseCash(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.BaseCashes.Where(f => f.Id == id).FirstOrDefaultAsync();
            if (item == null) return NotFound();

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
            return BadRequest(e.Message);
        }
    }
}