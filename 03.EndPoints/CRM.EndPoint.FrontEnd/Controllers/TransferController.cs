using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Transfers.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Policy = "NotCustomerOnly")]
public class TransferController(
    LogSystemService logSystemService,
    FileSaver fileSaver,
    DateTimeServices dtService,
    CRMContextDb db,
    UserManager<AppUser> userManager,
    IHubContext<TaskHub> hub) : Controller {
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GetAllTransfersDt()
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

        var baseQuery = db.Transfers.AsNoTracking();

        var recordsTotal = await baseQuery.CountAsync();

        if (!string.IsNullOrEmpty(searchValue))
        {
            var searchNumber = TextService.convertToInt(searchValue);
            baseQuery = baseQuery.Where(t =>
                t.TransferNumber == searchNumber ||
                t.Description.Contains(searchValue) ||
                t.ToAccountTxt.Contains(searchValue) ||
                t.FromAccountTxt.Contains(searchValue)
            );
        }

        var filterCounts = new Dictionary<int, int> {
            [-1] = await baseQuery.CountAsync(),
            [1] = await baseQuery.Where(f => f.Status == TransferStatusEnum.در_دست_تایید || f.Status == TransferStatusEnum.ثبت_اولیه).CountAsync(),
            [2] = await baseQuery.Where(f => f.Status == TransferStatusEnum.تایید_شده).CountAsync(),
            [3] = await baseQuery.Where(f => f.Status == TransferStatusEnum.رد_شده).CountAsync(),
        };


        var query = model?._type switch {
            1 => baseQuery.Where(f => f.Status == TransferStatusEnum.در_دست_تایید),
            2 => baseQuery.Where(f => f.Status == TransferStatusEnum.تایید_شده),
            3 => baseQuery.Where(f => f.Status == TransferStatusEnum.رد_شده),
            _ => baseQuery
        };

        var recordsFiltered = await query.CountAsync();

        if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortDirection))
        {
            var isAsc = sortDirection == "asc";

            query = sortColumn switch {
                "transferNumber" => isAsc ? query.OrderBy(c => c.TransferNumber) : query.OrderByDescending(c => c.TransferNumber),
                "amount" => isAsc ? query.OrderBy(c => c.Amount) : query.OrderByDescending(c => c.Amount),
                "createDateTxt" => isAsc ? query.OrderBy(c => c.CreateDate) : query.OrderByDescending(c => c.CreateDate),
                "fromAccountTxt" => isAsc ? query.OrderBy(c => c.FromAccountTxt) : query.OrderByDescending(c => c.FromAccountTxt),
                "toAccountTxt" => isAsc ? query.OrderBy(c => c.ToAccountTxt) : query.OrderByDescending(c => c.ToAccountTxt),
                "transferDate" => isAsc ? query.OrderBy(c => c.TransferDateMiladi) : query.OrderByDescending(c => c.TransferDateMiladi),
                _ => query.OrderByDescending(c => c.CreateDate)
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
            filterCounts
        });
    }


    [HttpPost]
    public async Task<IActionResult> AddTransfer([FromBody] Transfer model)
    {
        if (model == null) return BadRequest();

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var isCreate = model.Id == Guid.Empty;
            Transfer oldData = null;

            if (isCreate)
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
            } else
            {
                oldData = await db.Transfers.Where(f => f.Id == model.Id).AsNoTracking().FirstOrDefaultAsync();
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;
            }

            model.TransferDateMiladi = dtService.GetDateMiladi(model.TransferDate) ?? DateTime.Now;

            db.Transfers.Update(model);
            if (!isCreate)
                db.Entry(model).Property(f => f.TransferNumber).IsModified = false;

            await db.SaveChangesAsync();

            if (isCreate) await logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
            else await logSystemService.LogChange(oldData, model, LogActionEnum.ویرایش, user);


            var attach = fileSaver.SaveAttachments(model.Attachments, _AttachType.transfer);
            foreach (var att in attach ?? [])
            {
                att.EntityId = model.Id;
            }

            db.Attachments.UpdateRange(attach);

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


    public async Task<IActionResult> GetTransfer(Guid id)
    {
        try
        {
            var data = await db.Transfers.Where(f => f.Id == id).AsNoTracking()
                .Select(t => new {
                    Transfer = t,
                    Attachments = db.Attachments
                        .Where(a => a.EntityId == t.Id && a._AttachType == _AttachType.transfer)
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (data == null) return NotFound();

            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }


    public async Task<IActionResult> DeleteTransfer(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.Transfers.FirstOrDefaultAsync(f => f.Id == id);
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

    /*[HttpGet]
    public async Task<IActionResult> ChangeTransferStatus(Guid id, TransferStatusEnum newStatus)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var transfer = await db.Transfers.FirstOrDefaultAsync(f => f.Id == id);
            if (transfer == null) return NotFound();

            if (transfer.Status != newStatus)
            {
                var oldItem = await db.Transfers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == transfer.Id);

                transfer.Status = newStatus;

                transfer.UserModify = user.FullName;
                transfer.UserModifyID = user.Id;
                transfer.ModifyDate = DateTime.Now;
                transfer.ModifyDateTxt = dtService.GetDateTimePersian();

                await logSystemService.LogChange(oldItem, transfer, LogActionEnum.تغییر_وضعیت, user);
            }

            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }*/

    [HttpPatch]
    public async Task<IActionResult> ChangeTransferStatus(Guid id, [FromBody] TransferStatusEnum newStatus)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null) return Unauthorized();

            var transfer = await db.Transfers.FindAsync(id);
            if (transfer == null) return NotFound("نقل و انتقال مورد نظر یافت نشد.");

            if (transfer.Status == newStatus) return Ok();


            transfer.Status = newStatus;
            transfer.UserModify = user.FullName;
            transfer.UserModifyID = user.Id;
            transfer.ModifyDate = DateTime.Now;
            transfer.ModifyDateTxt = dtService.GetDateTimePersian();

            var oldItem = (Transfer)db.Entry(transfer).OriginalValues.ToObject();
            await logSystemService.LogChange(oldItem, transfer, LogActionEnum.تغییر_وضعیت, user);

            await db.SaveChangesAsync();
            await hub.Clients.Groups("مدیر عامل", "مدیر ارشد").SendAsync("countOfPendingCartable");
            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}