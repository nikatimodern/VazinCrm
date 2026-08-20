using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.Chats.Entities;
using CRM.EndPoint.FrontEnd.IServices.PushNotification;
using CRM.EndPoint.FrontEnd.Models.Chat;
using CRM.EndPoint.FrontEnd.Models.PushNotification;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class ChatController : Controller {
    private readonly DateTimeServices dtService;
    private readonly CRMContextDb db;
    private readonly UserContextDb udb;
    private readonly UserManager<AppUser> userManager;
    private readonly SignInManager<AppUser> signInManager;
    private readonly FileSaver fileSaver;
    private readonly IHubContext<TaskHub> hub;
    private readonly IPushQueue pushQueue;

    public ChatController(DateTimeServices dtService, CRMContextDb db, UserContextDb udb, UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager, FileSaver fileSaver, IHubContext<TaskHub> hub, IPushQueue pushQueue)
    {
        this.dtService = dtService;
        this.db = db;
        this.udb = udb;
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.fileSaver = fileSaver;
        this.hub = hub;
        this.pushQueue = pushQueue;
    }


    [Authorize]
    [HttpGet]
    public IActionResult Main()
    {
        return View();
    }


    public async Task<IActionResult> GetSalesExperts(string mobile)
    {
        var salesExpert = (await userManager.GetUsersInRoleAsync("کارشناس فروش")).Select(u => new {
            u.Id,
            u.FullName,
            u.PersonImageUrl
        }).ToArray();

        return Ok(salesExpert);
    }


    [Authorize]
    public async Task<IActionResult> ShowListChats()
    {
        var fromUser = await userManager.GetUserAsync(HttpContext.User);

        var mainChats = await db.MainChats
            .Where(f => f.FromUserId == fromUser.Id || f.ToUserId == fromUser.Id)
            .OrderByDescending(f => f.ModifyDate)
            .Include(f => f.Chats)
            .ToListAsync();


        var chatList = new List<ShowListChats>();
        foreach (var item in mainChats)
        {
            var targetUserId = fromUser?.Id == item.ToUserId ? item.FromUserId : item.ToUserId;
            var targetUserTxt = fromUser?.Id == item.ToUserId ? item.FromUserTxt : item.ToUserTxt;
            var userImage = await udb.Users.Where(f => f.Id == targetUserId).Select(u => u.PersonImageUrl).FirstOrDefaultAsync();
            var chat = new ShowListChats {
                FullName = targetUserTxt,
                PersonImageUrl = userImage,
                MainChatId = item.Id,
                CountNotRead = item.Chats.Count(f => f.UserCreatorID != fromUser?.Id && !f.Read)
            };
            chatList.Add(chat);
        }

        return Ok(chatList);
    }

    [Authorize]
    public async Task<IActionResult> ShowChat(Guid mainChatId)
    {
        var fromUser = await userManager.GetUserAsync(HttpContext.User);
        if (fromUser == null) return Unauthorized();

        var mainChat = await db.MainChats
            .Include(x => x.Chats.OrderBy(c => c.CreateDate))
            .FirstOrDefaultAsync(x => x.Id == mainChatId);

        if (mainChat == null) return NotFound();

        var userIds = mainChat.Chats.Select(x => x.UserCreatorID).Distinct().ToList();
        var userImages = await udb.Users
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.PersonImageUrl);

        var chatIds = mainChat.Chats.Select(x => x.Id).ToList();
        var attachments = await db.Attachments.Where(x => chatIds.Contains(x.EntityId.Value))
            .Select(x => new {
                x.EntityId,
                x.OriginalFilename,
                x.FileSize
            })
            .ToListAsync();

        var firstUnreadChatId = mainChat.Chats
            .Where(x => !x.Read && x.UserCreatorID != fromUser.Id)
            .Select(x => x.Id)
            .FirstOrDefault();


        var hasUnreadChats = false;
        foreach (var chat in mainChat.Chats)
        {
            userImages.TryGetValue(chat.UserCreatorID, out var image);
            chat.UserImage = image;

            var attachment = attachments.FirstOrDefault(x => x.EntityId == chat.Id);

            chat.FileName = attachment?.OriginalFilename ?? Path.GetFileName(chat.FileAddress);
            chat.FileSize = fileSaver.FormatFileSize(attachment?.FileSize);

            if (!chat.Read && chat.UserCreatorID != fromUser.Id)
            {
                chat.Read = true;
                hasUnreadChats = true;
            }
        }

        if (hasUnreadChats)
        {
            mainChat.ReminderSentUserId = null;
            mainChat.ReminderSentDate = null;


            var updateUserId = mainChat.FromUserId == fromUser.Id ? mainChat.ToUserId : mainChat.FromUserId;
            await db.SaveChangesAsync();
            await hub.Clients.User(updateUserId).SendAsync("UpdateChat", mainChat.Id);
        }


        HttpContext.Session.SetString("mainChatId", mainChat.Id.ToString());

        var groups = mainChat.Chats
            .GroupBy(x => x.CreateDate.Date)
            .Select(g => new ChatGroupVModel {
                DateTitle = GetChatDateTitle(g.Key),
                Chats = g.ToList()
            })
            .ToList();

        var vm = new ShowChatVModel {
            MainChat = mainChat,
            Groups = groups,
            CurrentUserId = fromUser.Id,
            FirstUnreadChatId = firstUnreadChatId
        };

        return PartialView(vm);
    }


    private string GetChatDateTitle(DateTime? date)
    {
        if (date == null) return "";

        var today = DateTime.Today;

        if (date.Value.Date == today)
            return "امروز";

        if (date.Value.Date == today.AddDays(-1))
            return "دیروز";

        return dtService.GetDatePersian(date.Value);
    }


    [Authorize]
    public async Task<IActionResult> CreateMainChat(string toUserId, string toUserTxt)
    {
        var user = await udb.Users.Where(f => f.PhoneNumber == toUserId).AsNoTracking().FirstOrDefaultAsync();
        if (user != null)
        {
            toUserId = user.Id;
            toUserTxt = user.FullName;
        }

        var fromUser = await userManager.GetUserAsync(HttpContext.User);
        if (string.IsNullOrEmpty(toUserTxt))
            toUserTxt = toUserId;


        var mainChat = await db.MainChats.Where(f =>
            (f.FromUserId == fromUser.Id && f.ToUserId == toUserId)
            || (f.ToUserId == fromUser.Id && f.FromUserId == toUserId)
        ).OrderBy(f => f.CreateDate).Include(f => f.Chats).FirstOrDefaultAsync();

        if (mainChat == null)
        {
            mainChat = new MainChat {
                FromUserId = fromUser.Id,
                FromUserTxt = fromUser.FullName,
                ToUserId = toUserId,
                ToUserTxt = toUserTxt,
            };
            db.MainChats.Add(mainChat);
            await db.SaveChangesAsync();
        }

        HttpContext.Session.SetString("mainChatId", mainChat.Id.ToString());
        return Ok(mainChat.Id);
    }


    [Authorize]
    [HttpPost]
    public async Task<IActionResult> SendChat([FromBody] SendChatVModel model)
    {
        if (model == null) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var fromUser = await userManager.GetUserAsync(HttpContext.User);
            if (fromUser == null) return Unauthorized();

            var mainChatIdStr = HttpContext.Session.GetString("mainChatId");
            if (!Guid.TryParse(mainChatIdStr, out var mainChatId)) return BadRequest();
            var mainChat = await db.MainChats.FirstOrDefaultAsync(f => f.Id == mainChatId);


            // EDIT
            if (model.Chat?.Id != Guid.Empty)
            {
                return await EditChat(model.Chat, mainChat, fromUser, transaction);
            }

            // ADD
            var text = model.Chat?.Text?.Trim();
            var chats = new List<Chat>();

            if (model.Attachments.Any())
            {
                var attachments = fileSaver.SaveAttachments(model.Attachments, _AttachType.Chat);

                foreach (var att in attachments ?? [])
                {
                    var chatId = Guid.NewGuid();

                    chats.Add(new Chat {
                        Id = chatId,
                        UserCreatorID = fromUser.Id,
                        UserCreator = fromUser.FullName,
                        MainChatId = mainChatId,
                        Text = text,
                        FileAddress = att.Url
                    });

                    att.EntityId = chatId;
                    att.UserCreator = fromUser.FullName;
                    att.UserCreatorID = fromUser.Id;
                }

                db.Attachments.AddRange(attachments);
            } else
            {
                chats.Add(new Chat {
                    UserCreatorID = fromUser.Id,
                    UserCreator = fromUser.FullName,
                    MainChatId = mainChatId,
                    Text = text
                });
            }

            db.Chats.AddRange(chats);


            if (mainChat != null)
            {
                mainChat.ModifyDate = DateTime.Now;
                mainChat.ModifyDateTxt = dtService.GetDateTimePersian();
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            await SendChatNotification(mainChat, fromUser, text);

            return Ok(mainChatId);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            Console.WriteLine(e);
            throw;
        }
    }

    [Authorize]
    private async Task<IActionResult> EditChat(Chat editedChat, MainChat mainChat, AppUser fromUser, IDbContextTransaction transaction)
    {
        var chat = await db.Chats.FirstOrDefaultAsync(x => x.Id == editedChat.Id);

        if (chat == null) return NotFound();

        chat.Text = editedChat?.Text?.Trim();
        chat.IsEdited = true;
        chat.EditDate = DateTime.Now;
        chat.EditDateTxt = dtService.GetDateTimePersian();

        await db.SaveChangesAsync();

        if (mainChat != null)
        {
            var updateUserId = mainChat.FromUserId == fromUser?.Id ? mainChat.ToUserId : mainChat.FromUserId;
            await hub.Clients.User(updateUserId).SendAsync("UpdateChat", mainChat.Id);
        }

        await transaction.CommitAsync();
        return Ok();
    }

    private async Task SendChatNotification(MainChat mainChat, AppUser fromUser, string text)
    {
        if (mainChat == null) return;
        var notifUserId = mainChat.FromUserId == fromUser?.Id ? mainChat.ToUserId : mainChat.FromUserId;

        const int maxLength = 50;
        var displayText = "";
        if (!string.IsNullOrEmpty(text))
        {
            displayText = text.Length > maxLength ? $"{text[..maxLength]}..." : text;
        } else if (mainChat.Chats.Any(f => f.FileAddress != null))
        {
            displayText = "فایل";
        }

        await hub.Clients.User(notifUserId).SendAsync("NotifChat", mainChat.Id, $"{fromUser?.FullName}: {displayText}".Trim());

        pushQueue?.Enqueue(new PushQueueItem {
            UserId = notifUserId,
            Title = fromUser?.FullName ?? "پیام رسان",
            Body = displayText,
            Url = "/Chat/Main",
            Tag = "chat" + fromUser?.FullName
        });
    }

    [Authorize]
    public async Task<IActionResult> DeleteChat(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.Chats.FirstOrDefaultAsync(f => f.Id == id);
            if (item == null) return NotFound();

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;

            if (item.FileAddress != null)
            {
                var att = await db.Attachments.FirstOrDefaultAsync(f => f.EntityId == item.Id);
                if (att != null)
                {
                    fileSaver.DeleteFile(att.Url);
                    att.IsDeleted = true;
                    att.ModifyDate = DateTime.Now;
                    att.ModifyDateTxt = dtService.GetDateTimePersian();
                    att.UserModify = user.FullName;
                    att.UserModifyID = user.Id;
                }
            }

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}