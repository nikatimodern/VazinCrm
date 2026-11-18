using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sms.Enums;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.Sms;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class SmsController(SmsService smsService, CRMContextDb db) : Controller {
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        return Json(await smsService.GetDashboardAsync());
    }

    [HttpPost]
    public async Task<IActionResult> GetReportsDt()
    {
        try
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "20");
            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.Trim().ToLower();
            var patternTypeValue = Request.Form["patternType"].FirstOrDefault();

            var query = db.SentSmses
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAtFaraz)
                .AsQueryable();

            var recordsTotal = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                // Look up contact numbers only when a name can be part of the search.
                var contactMobiles = searchValue.Any(char.IsLetter)
                    ? await smsService.SearchContactMobileNumbersAsync(searchValue, HttpContext.RequestAborted)
                    : [];
                var iranMobileSuffixes = contactMobiles
                    .Where(TextService.IsValidPhoneNumber)
                    .Select(x => x[1..])
                    .ToArray();
                var otherMobiles = contactMobiles
                    .Where(x => !TextService.IsValidPhoneNumber(x))
                    .ToArray();

                if (searchValue.Length < 3)
                {
                    query = query.Where(x =>
                        x.Mobile == searchValue ||
                        x.MessageText == searchValue ||
                        x.FarazId.ToString() == searchValue ||
                        x.Number == searchValue ||
                        (x.ReceivedFullName != null && x.ReceivedFullName.Contains(searchValue)) ||
                        ((x.ReceivedFullName == null || x.ReceivedFullName.Trim() == "") && x.Mobile != null &&
                            ((x.Mobile.Length >= 10 && iranMobileSuffixes.Contains(x.Mobile.Substring(x.Mobile.Length - 10))) ||
                             otherMobiles.Contains(x.Mobile)))
                    );
                }
                else
                {
                    query = query.Where(x =>
                        (x.Mobile != null && x.Mobile.Contains(searchValue)) ||
                        (x.MessageText != null && x.MessageText.Contains(searchValue)) ||
                        x.FarazId.ToString().Contains(searchValue) ||
                        (x.Number != null && x.Number.Contains(searchValue)) ||
                        (x.ReceivedFullName != null && x.ReceivedFullName.Contains(searchValue)) ||
                        ((x.ReceivedFullName == null || x.ReceivedFullName.Trim() == "") && x.Mobile != null &&
                            ((x.Mobile.Length >= 10 && iranMobileSuffixes.Contains(x.Mobile.Substring(x.Mobile.Length - 10))) ||
                             otherMobiles.Contains(x.Mobile)))
                    );
                }
            }

            if (!string.IsNullOrWhiteSpace(patternTypeValue) && Enum.TryParse<SmsPatternType>(patternTypeValue, out var patternType))
            {
                query = query.Where(x => x.PatternType == patternType);
            }


            var recordsFiltered = await query.CountAsync();

            if (length != -1)
                query = query.Skip(start).Take(length);

            var data = await query.ToListAsync();
            var unnamed = data.Where(x => string.IsNullOrWhiteSpace(x.ReceivedFullName)).ToList();
            if (unnamed.Count > 0)
            {
                var names = await smsService.GetContactNamesAsync(
                    unnamed.Select(x => x.Mobile), HttpContext.RequestAborted);
                foreach (var sms in unnamed)
                    sms.ReceivedFullName = names.GetValueOrDefault(TextService.NormalizeIranMobile(sms.Mobile));
            }

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered,
                data
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetReceivesDt()
    {
        try
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "20");
            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.Trim();

            var query = db.ReceivedSmses
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAtFaraz)
                .AsQueryable();

            var recordsTotal = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                // Look up contact numbers only when a name can be part of the search.
                var contactMobiles = searchValue.Any(char.IsLetter)
                    ? await smsService.SearchContactMobileNumbersAsync(searchValue, HttpContext.RequestAborted)
                    : [];
                var iranMobileSuffixes = contactMobiles
                    .Where(TextService.IsValidPhoneNumber)
                    .Select(x => x[1..])
                    .ToArray();
                var otherMobiles = contactMobiles
                    .Where(x => !TextService.IsValidPhoneNumber(x))
                    .ToArray();

                if (searchValue.Length < 3)
                {
                    query = query.Where(x =>
                        x.Mobile == searchValue ||
                        x.MessageText == searchValue ||
                        x.FarazId.ToString() == searchValue ||
                        x.Number == searchValue ||
                        (x.SenderFullName != null && x.SenderFullName.Contains(searchValue)) ||
                        ((x.SenderFullName == null || x.SenderFullName.Trim() == "") && x.Mobile != null &&
                            ((x.Mobile.Length >= 10 && iranMobileSuffixes.Contains(x.Mobile.Substring(x.Mobile.Length - 10))) ||
                             otherMobiles.Contains(x.Mobile)))
                    );
                }
                else
                {
                    query = query.Where(x =>
                        (x.Mobile != null && x.Mobile.Contains(searchValue)) ||
                        (x.MessageText != null && x.MessageText.Contains(searchValue)) ||
                        x.FarazId.ToString().Contains(searchValue) ||
                        (x.Number != null && x.Number.Contains(searchValue)) ||
                        (x.SenderFullName != null && x.SenderFullName.Contains(searchValue)) ||
                        ((x.SenderFullName == null || x.SenderFullName.Trim() == "") && x.Mobile != null &&
                            ((x.Mobile.Length >= 10 && iranMobileSuffixes.Contains(x.Mobile.Substring(x.Mobile.Length - 10))) ||
                             otherMobiles.Contains(x.Mobile)))
                    );
                }
            }


            var recordsFiltered = await query.CountAsync();

            if (length != -1)
                query = query.Skip(start).Take(length);

            var data = await query.ToListAsync();
            var unnamed = data.Where(x => string.IsNullOrWhiteSpace(x.SenderFullName)).ToList();
            if (unnamed.Count > 0)
            {
                var names = await smsService.GetContactNamesAsync(
                    unnamed.Select(x => x.Mobile), HttpContext.RequestAborted);
                foreach (var sms in unnamed)
                    sms.SenderFullName = names.GetValueOrDefault(TextService.NormalizeIranMobile(sms.Mobile));
            }

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered,
                data
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }


    [HttpPost]
    public async Task<IActionResult> SyncReceives(CancellationToken cancellationToken)
    {
        try
        {
            await smsService.SyncReceiveSmsAsync(cancellationToken);

            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }


    [HttpPost]
    public async Task<IActionResult> SyncSents(CancellationToken cancellationToken)
    {
        try
        {
            await smsService.SyncSentSmsAsync(cancellationToken);

            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }
}