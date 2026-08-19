using System;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Services;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class EmailController(CRMContextDb db, UserManager<AppUser> userManager, UserContextDb udb, Service genService, DateTimeServices dtService)
    : Controller {
    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> GetAllEmails(int type, string search)
    {
        try
        {
            var userId = Guid.Parse(userManager.GetUserId(HttpContext.User));
            var emailsQuery = db.Emails.AsNoTracking().AsQueryable();
            var emailsCount = await emailsQuery.Where(e => e.ToId == userId && !e.Seen).CountAsync();

            emailsQuery = type switch {
                1 => emailsQuery.Where(e => e.ToId == userId),
                2 => emailsQuery.Where(e => e.FromId == userId),
                _ => null
            };

            if (!string.IsNullOrEmpty(search))
            {
                emailsQuery = emailsQuery.Where(e =>
                    (type == 1 && e.FromName.Contains(search))
                    || (type == 2 && e.ToName.Contains(search))
                    || e.Title.Contains(search)
                    || e.Message.Contains(search)
                );
            }

            var emails = await emailsQuery
                .OrderByDescending(e => e.CreateDate)
                .Select(e => new {
                    e.Id,
                    e.Title,
                    e.FromName,
                    e.ToName,
                    e.CreateDateTxt,
                    e.Seen,
                })
                .ToListAsync();

            var data = new {
                Emails = emails,
                CountNotSeen = emailsCount
            };

            return Ok(data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<IActionResult> GetEmail(Guid id, int type)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var email = await db.Emails.Where(e => e.Id == id).FirstOrDefaultAsync();
            if (email == null) return NotFound();

            if (type == 1)
            {
                email.Seen = true;
                email.ModifyDate = DateTime.Now;
                email.ModifyDateTxt = dtService.GetDateTimePersian();
                email.UserModifyID = user.Id;
                email.UserModify = user.FullName;
            }

            var userNameId = type == 1 ? email.FromId.ToString() : email.ToId.ToString();
            email.UserImage = (await udb.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userNameId))?.PersonImageUrl;

            await db.SaveChangesAsync();

            var emailsCount = await genService.GetEmailsNotSeenCount(Guid.Parse(user.Id));
            var data = new {
                Email = email,
                CountNotSeen = emailsCount
            };
            return Ok(data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}