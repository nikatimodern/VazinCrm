using System;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Transfers.Entities;
using CRM.EndPoint.FrontEnd.Services;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Policy = "NotCustomerOnly")]
public class TransferController(
    LogSystemService logSystemService,
    FileSaver fileSaver,
    DateTimeServices dtService,
    CRMContextDb db,
    UserManager<AppUser> userManager) : Controller {
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTransfersDt()
    {
        var draw = Request.Query["draw"].FirstOrDefault();
        var start = Convert.ToInt32(Request.Query["start"].FirstOrDefault() ?? "0");
        var length = Convert.ToInt32(Request.Query["length"].FirstOrDefault() ?? "10");
        var searchValue = Request.Query["search[value]"].FirstOrDefault();
        var sortColumnIndex = Request.Query["order[0][column]"].FirstOrDefault();
        var sortDirection = Request.Query["order[0][dir]"].FirstOrDefault();
        var sortColumn = Request.Query[$"columns[{sortColumnIndex}][data]"].FirstOrDefault();

        var query = db.Transfers.AsNoTracking();

        var recordsTotal = await query.CountAsync();

        if (!string.IsNullOrEmpty(searchValue))
        {
            var searchNumber = TextService.convertToInt(searchValue);
            query = query.Where(t =>
                t.TransferNumber == searchNumber ||
                t.Description.Contains(searchValue)
            );
        }

        var recordsFiltered = await query.CountAsync();

        if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortDirection))
        {
            var isAsc = sortDirection == "asc";

            query = sortColumn switch {
                "searchNumber" => isAsc ? query.OrderBy(c => c.TransferNumber) : query.OrderByDescending(c => c.TransferNumber),
                "amount" => isAsc ? query.OrderBy(c => c.Amount) : query.OrderByDescending(c => c.Amount),
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
            data
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
}