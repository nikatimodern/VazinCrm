using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.Chats.Entities;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models.Chats;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Login;
using CRM.EndPoint.FrontEnd.Models.ViewModel.PushNotification;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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



    [HttpGet]
    public IActionResult ChatLogin()
    {
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> ChatLogin([FromBody] LoginOtpRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
                return Ok(new { status = 405, error });
            }

            var mobile = model.Mobile;
            var user = await userManager.FindByNameAsync(mobile) ?? await udb.Users.Where(f => f.PhoneNumber == mobile).FirstOrDefaultAsync();

            if (user == null)
            {
                user = new AppUser {
                    UserName = mobile,
                    PhoneNumber = mobile,
                    FirstName = mobile
                };
                var result = await userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    await signInManager.SignOutAsync();
                    await signInManager.SignInAsync(user, false);

                    var mainChats = await db.MainChats.Where(f => f.ToUserId == mobile).ToListAsync();
                    foreach (var item in mainChats)
                    {
                        item.ToUserId = user.Id;
                    }
                }

                await db.SaveChangesAsync();
            } else
            {
                await signInManager.SignOutAsync();
                await signInManager.SignInAsync(user, false);
            }

            var url = Url.Action("Main", "Chat");
            return Ok(new { status = 200, url });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet]
    public IActionResult Main()
    {
        //var user = userManager.GetUserAsync(HttpContext.User).Result;

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

    public async Task<IActionResult> ShowChat(Guid mainChatId)
    {
        var fromUser = await userManager.GetUserAsync(HttpContext.User);

        var mainChat = await db.MainChats
            .Where(f => f.Id == mainChatId)
            .OrderBy(f => f.CreateDate)
            .Include(f => f.Chats)
            .FirstOrDefaultAsync();


        foreach (var chat in mainChat.Chats)
        {
            var userImage = await udb.Users.Where(f => f.Id == chat.UserCreatorID).Select(u => u.PersonImageUrl).FirstOrDefaultAsync();
            chat.UserImage = userImage;
            if (fromUser.Id != chat.UserCreatorID)
            {
                chat.Read = true;
            }
        }

        await db.SaveChangesAsync();

        HttpContext.Session.SetString("mainChatId", mainChat.Id.ToString());
        ViewBag.FromUserId = fromUser.Id;
        return PartialView(mainChat);
    }

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


    [HttpPost]
    public async Task<IActionResult> SendChat([FromBody] SendChatVModel model)
    {
        try
        {
            var fromUser = await userManager.GetUserAsync(HttpContext.User);
            var mainChatId = HttpContext.Session.GetString("mainChatId");


            if (model.Attachments.Any())
            {
                var attachments = fileSaver.saveAttach(model.Attachments, _AttachType.Chat);
                foreach (var item in attachments)
                {
                    var chat = new Chat {
                        UserCreatorID = fromUser.Id,
                        UserCreator = fromUser.FullName,
                        MainChatId = Guid.Parse(mainChatId),
                        Text = string.IsNullOrWhiteSpace(model.Chat?.Text) ? "فایل" : model.Chat.Text,
                        FileAddress = item.Url
                    };
                    db.Chats.Add(chat);
                    item.EntityId = chat.Id;
                }
            } else
            {
                var chat = new Chat {
                    UserCreatorID = fromUser?.Id,
                    UserCreator = fromUser?.FullName,
                    MainChatId = Guid.Parse(mainChatId),
                    Text = model.Chat?.Text,
                };
                db.Chats.Add(chat);
            }

            var mainChat = await db.MainChats.Where(f => f.Id.ToString() == mainChatId).FirstOrDefaultAsync();

            mainChat?.ModifyDate = DateTime.Now;
            mainChat?.ModifyDateTxt = dtService.GetDateTimePersian();


            var notifUserId = mainChat?.FromUserId == fromUser?.Id ? mainChat?.ToUserId : mainChat?.FromUserId;

            const int maxLength = 50;
            var text = model.Chat?.Text ?? "";
            var displayText = text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text;

            await hub.Clients.User(notifUserId).SendAsync("Chat", mainChatId, $"{fromUser?.FullName}: {displayText}".Trim());

            var rnd = new Random();
            pushQueue.Enqueue(new PushQueueItem {
                UserId = notifUserId,
                Title = fromUser?.FullName ?? "پیام رسان",
                Body = displayText,
                Url = "/Chat/Main",
                Tag = "chat" + fromUser?.FullName
            });

            await db.SaveChangesAsync();
            return Ok(mainChatId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<IActionResult> DeleteChat(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.Chats.Where(f => f.Id == id).FirstOrDefaultAsync();
            if (item == null) return NotFound();

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;

            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

}