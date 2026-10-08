using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.CallRecordings.Entities;
using CRM.Core.Domain.CallRecordings.Enums;
using CRM.Core.Domain.Customers.Entities;
using CRM.EndPoint.FrontEnd.IServices.Ai;
using CRM.EndPoint.FrontEnd.Models.Asterisk;
using CRM.EndPoint.FrontEnd.Models.Configuration;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.Asterisk;

public class AsteriskAmiListener : BackgroundService {
    private readonly IHubContext<CallHub> _hubContext;
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IAiQueue _aiQueue;
    private readonly ILogger<AsteriskAmiListener> logger;

    public AsteriskAmiListener(IHubContext<CallHub> hubContext, IConfiguration config, IServiceScopeFactory serviceScopeFactory, IAiQueue aiQueue,
        ILogger<AsteriskAmiListener> logger)
    {
        _hubContext = hubContext;
        _config = config;
        _serviceScopeFactory = serviceScopeFactory;
        _aiQueue = aiQueue;
        this.logger = logger;
    }

    private readonly ConcurrentDictionary<string, CallSessionModel> _calls = new();

    private readonly string[] test = [
        "varset", "newexten", "devicestatechange", "newchannel", "newstate",
        "newconnectedline", "extensionstatus", "dialstate", "rtcpreceived", "rtcpsent"
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = _config.GetSection("voipConfig").Get<AppSettingVoip>();

        if (config?.ActiveStatus != true)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            AsteriskAMI ami = null;

            try
            {
                logger.LogInformation("[AMI] Connecting to {Host}:{Port}",
                    config._hostname,
                    config._port);

                ami = new AsteriskAMI(
                    config._hostname,
                    config._port,
                    config._username,
                    config._password);

                await ami.ConnectAsync();

                logger.LogInformation("[AMI] Connected successfully");

                while (!stoppingToken.IsCancellationRequested)
                {
                    var raw = await ami.ReadRawAsync();

                    if (string.IsNullOrWhiteSpace(raw))
                        throw new Exception("AMI connection closed.");

                    var events = ParseAmiResponse(raw);

                    foreach (var ev in events ?? [])
                    {
                        await HandleEvent(ev);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[AMI] Connection failed/lost. Reconnecting in 5 seconds...");

                logger.LogError($"[AMI] Connection failed/lost: {ex.Message}");
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        logger.LogInformation("[AMI] Listener stopped");
    }

    private async Task HandleEvent(Dictionary<string, string> ev)
    {
        var evt = Get(ev, "Event")?.ToLowerInvariant();

        switch (evt)
        {
            case "dialbegin":
                await HandleDialBegin(ev);
                break;

            case "bridgeenter":
                await HandleBridgeEnter(ev);
                break;

            case "varset":
                HandleVarSet(ev);
                break;

            case "hangup":
                await HandleHangup(ev);
                break;
        }
    }


    private async Task HandleDialBegin(Dictionary<string, string> ev)
    {
        try
        {
            var uniqueId = Get(ev, "Uniqueid");
            var linkedId = Get(ev, "Linkedid") ?? uniqueId;

            var destUid = Get(ev, "DestUniqueid");

            if (string.IsNullOrEmpty(uniqueId) && string.IsNullOrEmpty(destUid)) return;

            var number = GetSrc(ev);

            if (string.IsNullOrEmpty(number) || number == "unknown") return;

            var dest = Get(ev, "DestCallerIDNum"); //DestExten

            var srcIsInternal = IsInternal(number);

            var userDest = await GetUsers(dest);
            if (userDest == null) return;

            var srcText = await GetSrcText(srcIsInternal, number);

            if (dest.StartsWith("1"))
            {
                await _hubContext.Clients.User(userDest.Id)
                    .SendAsync("IncomingCall", srcText, number, dest, !string.IsNullOrEmpty(destUid) ? destUid : uniqueId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"DialBegin Error: {ex.Message}");
        }
    }

    private void HandleVarSet(Dictionary<string, string> ev)
    {
        try
        {
            var variable = Get(ev, "Variable");
            if (variable != "MIXMONITOR_FILENAME") return;

            var uniqueId = Get(ev, "Uniqueid");
            var linkedId = Get(ev, "Linkedid") ?? uniqueId;
            var filePath = Get(ev, "Value");

            if (string.IsNullOrWhiteSpace(uniqueId) || string.IsNullOrWhiteSpace(filePath))
            {
                logger.LogInformation("VarSet: uniqueId or filePath is null: uniqueId:{uniqueId}, filePath:{filePath}", uniqueId, filePath);
                return;
            }

            _calls.AddOrUpdate(linkedId,
                _ => new CallSessionModel {
                    LinkedId = linkedId,
                    RecordingPath = filePath
                },
                (_, existing) => {
                    existing.RecordingPath = filePath;
                    return existing;
                }
            );
            logger.LogInformation("VarSet: filePath: {FilePath}", filePath.Split("/").Last());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"VarSet Error: {ex.Message}");
            Console.WriteLine(ex);
        }
    }

    private async Task HandleBridgeEnter(Dictionary<string, string> ev)
    {
        try
        {
            var uniqueId = Get(ev, "Uniqueid");
            var linkedId = Get(ev, "Linkedid") ?? uniqueId;
            var destUid = Get(ev, "DestUniqueid");

            if (!string.IsNullOrEmpty(uniqueId) || !string.IsNullOrEmpty(destUid))
            {
                await _hubContext.Clients.All.SendAsync("CallAnswered", !string.IsNullOrEmpty(destUid) ? destUid : uniqueId);
            }

            if (_calls.TryGetValue(linkedId, out var call))
            {
                call.Answered = true;
                call.AnswerTime ??= DateTime.Now;

                var callerIdNum = Get(ev, "CallerIDNum");
                var connectedLineNum = Get(ev, "ConnectedLineNum");

                string exten = null;
                if (IsInternal(connectedLineNum))
                {
                    exten = connectedLineNum;
                } else if (IsInternal(callerIdNum))
                {
                    exten = callerIdNum;
                }


                if (!string.IsNullOrEmpty(exten) && !call.Participants.Any(p => p.Extension == exten))
                {
                    call.Participants.Add(new CallParticipant() { Extension = exten, AnswerTime = DateTime.Now });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BridgeEnter Error: {ExMessage}", ex.Message);
            Console.WriteLine($"BridgeEnter Error: {ex.Message}");
        }
    }

    private async Task HandleHangup(Dictionary<string, string> ev)
    {
        try
        {
            var uniqueId = Get(ev, "Uniqueid");
            var linkedId = Get(ev, "Linkedid") ?? uniqueId;
            var destUid = Get(ev, "DestUniqueid");


            if (string.IsNullOrEmpty(uniqueId) && string.IsNullOrEmpty(destUid))
            {
                logger.LogWarning("hangup: uniqueId and destUid is null: uniqueId:{uniqueId}, destUid:{destUid}", uniqueId, destUid);
                return;
            }

            await _hubContext.Clients.All.SendAsync("CallEnded", !string.IsNullOrEmpty(destUid) ? destUid : uniqueId);

            if (uniqueId != linkedId) return;

            if (!_calls.TryGetValue(linkedId, out var call)) return;


            if (!call.Answered || string.IsNullOrWhiteSpace(call.RecordingPath) || !call.Participants.Any())
            {
                logger.LogWarning("hangup: call not Answered or path is null: {FilePath}", call.RecordingPath?.Split("/").Last());
                _calls.TryRemove(linkedId, out _);
                return;
            }

            var duration = call.AnswerTime.HasValue ? DateTime.Now - call.AnswerTime.Value : TimeSpan.Zero;
            if (duration.TotalSeconds < 6)
            {
                logger.LogInformation("Call ignored. Duration={Duration}", duration.TotalSeconds);
                _calls.TryRemove(linkedId, out _);
                return;
            }

            var src = GetSrc(ev);
            if (IsInternal(src))
            {
                logger.LogInformation("hangup: the call is not external to internal: {FilePath}", call.RecordingPath.Split("/").Last());
                return;
            }

            //await EnqueueCallAsync(call, src, duration);

            _calls.TryRemove(linkedId, out _);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Hangup Error: {ex.Message}");
        }
    }

    private async Task EnqueueCallAsync(CallSessionModel call, string src, TimeSpan duration)
    {
        var firstParticipant = call.Participants.First();
        var user = await GetUsers(firstParticipant.Extension);
        if (user == null)
        {
            logger.LogWarning("hangup: user is null: {dest}", firstParticipant.Extension);
        }

        var callRecording = new CallRecording {
            CallerNumber = src,
            DestinationNumber = firstParticipant.Extension,
            DestinationUserId = user?.Id,
            DestinationUserTxt = user?.FullName,
            RecordingFilePath = call.RecordingPath,
            DurationSeconds = (int)duration.TotalSeconds,
            AiStatus = AiProcessStatusEnum.Enqueued
        };

        foreach (var p in call.Participants)
        {
            var u = await GetUsers(p.Extension);
            callRecording.Participants?.Add(new CallParticipant {
                Extension = p.Extension,
                AnswerTime = p.AnswerTime,
                UserId = u?.Id,
                UserFullName = u?.FullName
            });
        }

        var customer = GetCallerCustomer(src);
        if (customer != null)
        {
            var customerName = $"{customer.CompanyName} {customer.AgentName}".Trim();
            callRecording.CallerCustomerId = customer.Id;
            callRecording.CallerCustomerTxt = $"{customerName} (کد مشتری: {customer.CustomerCode})";
            callRecording.CallerCustomerCode = customer.CustomerCode;
        }

        logger.LogInformation("hangup and Enqueue: {FilePath}", call.RecordingPath?.Split("/").Last());

        _aiQueue?.Enqueue(callRecording);
    }


    private static string GetSrc(Dictionary<string, string> ev)
    {
        var src = Get(ev, "CallerIDNum");
        if (src == "9506")
            src = Get(ev, "Channel")?.Substring(4, 3);
        return src;
    }

    private static bool IsInternal(string n) => !string.IsNullOrWhiteSpace(n) && n.Length <= 3;

    private async Task<string> GetSrcText(bool isInternal, string src)
    {
        string srcText;
        if (isInternal)
        {
            var srcUser = await GetUsers(src);
            srcText = srcUser == null ? src : $"{srcUser.FullName} ({src})";
        } else
        {
            srcText = GetCallerText(src);
        }

        return srcText;
    }

    private async Task<AppUser> GetUsers(string num)
    {
        var users = await GetUsersCacheAsync();
        var user = users.FirstOrDefault(u =>
            (u.InternalTels ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Contains(num)
        );
        return user;
    }

    private List<AppUser> _usersCache = [];
    private DateTime _usersCacheTime = DateTime.MinValue;
    private readonly Lock _usersLock = new();

    private async Task<List<AppUser>> GetUsersCacheAsync()
    {
        if (_usersCache.Count > 0 && (DateTime.Now - _usersCacheTime).TotalMinutes < 5)
            return _usersCache;

        lock (_usersLock)
        {
            if (_usersCache.Count > 0 && (DateTime.Now - _usersCacheTime).TotalMinutes < 5)
                return _usersCache;
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserContextDb>();

        var users = db.Users.Where(f => f.IsEmployee).ToList();

        lock (_usersLock)
        {
            _usersCache = users;
            _usersCacheTime = DateTime.Now;
        }

        return users;
    }


    private static Dictionary<string, Customer> _cusCache = new();
    private static DateTime _lastCacheTime = DateTime.MinValue;

    private Customer GetCallerCustomer(string num)
    {
        if (string.IsNullOrEmpty(num)) return null;

        var key = num.Length > 7 ? num[^7..] : num;

        // هر 1 دقیقه کش آپدیت بشه
        if ((DateTime.Now - _lastCacheTime).TotalSeconds > 60 || _cusCache.Count == 0)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CRMContextDb>();

            var list = db.Customers.Select(c => new {
                c.Id,
                c.CustomerCode,
                c.CompanyName,
                c.AgentName,
                c.Tel,
                c.Mobile
            }).ToList();

            var dict = new Dictionary<string, Customer>(list.Count * 2);

            foreach (var c in list)
            {
                var customer = new Customer {
                    CompanyName = c.CompanyName,
                    AgentName = c.AgentName,
                    Id = c.Id,
                    CustomerCode = c.CustomerCode,
                };

                if (!string.IsNullOrEmpty(c.Tel))
                {
                    var k = c.Tel.Length > 7 ? c.Tel[^7..] : c.Tel;
                    dict.TryAdd(k, customer);
                }

                if (!string.IsNullOrEmpty(c.Mobile))
                {
                    var k = c.Mobile.Length > 7 ? c.Mobile[^7..] : c.Mobile;
                    dict.TryAdd(k, customer);
                }
            }

            _cusCache = dict;
            _lastCacheTime = DateTime.Now;
        }

        return _cusCache.TryGetValue(key, out var cus) ? cus : null;
    }

    private string GetCallerText(string num)
    {
        if (string.IsNullOrEmpty(num)) return num;

        var cus = GetCallerCustomer(num);

        return cus != null
            ? $"{cus.CompanyName} {cus.AgentName} ({num})"
            : num;
    }


    private static IEnumerable<Dictionary<string, string>> ParseAmiResponse(string chunk)
    {
        chunk = chunk.Replace("\r\n", "\n");
        var frames = chunk.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        foreach (var frame in frames)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var lines = frame.Split('\n');

            foreach (var line in lines)
            {
                var idx = line.IndexOf(':');
                if (idx <= 0) continue;

                var key = line.Substring(0, idx).Trim();
                var value = line.Substring(idx + 1).Trim();
                dict[key] = value;
            }

            if (dict.Count > 0)
                yield return dict;
        }
    }

    private static string Get(Dictionary<string, string> d, string key) =>
        d.TryGetValue(key, out var v) ? v : string.Empty;
}