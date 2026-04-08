using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CRM.EndPoint.FrontEnd.Services;

public class AsteriskAmiListener : BackgroundService {
    private readonly IHubContext<CallHub> _hubContext;
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IList<AppUser> _users;

    public AsteriskAmiListener(IHubContext<CallHub> hubContext, IConfiguration config, IServiceScopeFactory serviceScopeFactory) {
        _hubContext = hubContext;
        _config = config;
        this.serviceScopeFactory = serviceScopeFactory;

        using (var scope = serviceScopeFactory.CreateScope()) {
            var udb = scope.ServiceProvider.GetRequiredService<UserContextDb>();

            _users = udb.Users.ToList();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var voipConfig = _config.GetSection("voipConfig").Get<AppSettingVoip>();
        if (voipConfig.ActiveStatus != true) return;

        string[] test = new string[11];
        test[0] = "varset";
        test[1] = "newexten";
        test[2] = "devicestatechange";
        test[3] = "newchannel";
        test[4] = "newstate";
        test[5] = "newconnectedline";
        test[6] = "extensionstatus";
        test[7] = "dialstate";
        test[8] = "rtcpreceived";
        test[9] = "rtcpsent";


        var ami = new AsteriskAMI(voipConfig._hostname, voipConfig._port, voipConfig._username, voipConfig._password);
        try {
            await ami.ConnectAsync();
        }
        catch (Exception ex) {
            Console.WriteLine($"[AMI] اتصال برقرار نشد: {ex.Message}");
            return;
        }

        try {
            while (!stoppingToken.IsCancellationRequested) {
                var res = await ami.ReadRawAsync();

                foreach (var ev in ParseAmiResponse(res)) {
                    var evt = Get(ev, "Event");
                    if (!test.Contains(evt.ToLowerInvariant())) {
                        Console.WriteLine(evt);
                    }

                    switch (evt.ToLowerInvariant()) {
                        case "dialbegin": {
                            var srcCaller = Get(ev, "CallerIDNum");
                            if (srcCaller == "9506") {
                                srcCaller = Get(ev, "Channel").Substring(4, 3);
                            }

                            if (string.IsNullOrEmpty(srcCaller) || srcCaller == "unknown") continue;

                            var destCaller = Get(ev, "DestCallerIDNum"); //DestExten
                            bool srcIsInternal = srcCaller.Length <= 4;
                            bool destIsInternal = destCaller.Length <= 4;

                            /*string callType = null;
                             DestUniqueid
                            if (srcIsInternal && destIsInternal)
                                callType = "internal-to-internal";
                            else if (destCaller == "600")
                                callType = "external-to-ring";
                            else if (!srcIsInternal && destIsInternal && destCaller != "600")
                                callType = "external-to-internal";
                            else if (srcIsInternal && !destIsInternal)
                                callType = "internal-to-external";*/

                            string srcCallerText = null;
                            if (srcIsInternal) {
                                var srcUser = await GetUsers(srcCaller);
                                srcCallerText = srcUser == null ? srcCaller : $"{srcUser.FullName} ({srcCaller})";
                            } else if (!srcIsInternal) {
                                srcCallerText = GetCallerText(srcCaller);
                            }

                            /*string destCallerText = null;
                            if (!destIsInternal) {
                                var destCallerLast = destCaller.Substring(srcCaller.Length - 7);
                                var destCus = db.Customers.FirstOrDefault(f => destCallerLast.Contains(f.Tel) || destCallerLast.Contains(f.Mobile));
                                destCallerText = destCus == null ? srcCaller : destCus.CompanyName + " " + destCus.AgentName + " (" + destCus.CustomerCode + ")";
                            }*/

                            var desUser = await GetUsers(destCaller);

                            var uniqueid = Get(ev, "Uniqueid");
                            //var linkedId = Get(ev, "Linkedid");
                            var destUid = Get(ev, "DestUniqueid");

                            if (!string.IsNullOrEmpty(uniqueid) && desUser != null) {
                                await _hubContext.Clients
                                    .User(desUser.Id)
                                    .SendAsync("IncomingCall", srcCallerText, destCaller, !string.IsNullOrEmpty(destUid) ? destUid : uniqueid);
                            }

                            break;
                        }

                        case "hangup": {
                            var uniqueid = Get(ev, "Uniqueid");
                            //var linkedId = Get(ev, "LinkedId");
                            var destUid = Get(ev, "DestUniqueid");

                            if (!string.IsNullOrEmpty(uniqueid) || !string.IsNullOrEmpty(destUid)) {
                                await _hubContext.Clients.All.SendAsync("CallEnded", !string.IsNullOrEmpty(destUid) ? destUid : uniqueid);
                            }

                            break;
                        }

                        case "bridgeenter": {
                            var uniqueid = Get(ev, "Uniqueid");
                            //var linkedId = Get(ev, "LinkedId");
                            var destUid = Get(ev, "DestUniqueid");

                            if (!string.IsNullOrEmpty(uniqueid) || !string.IsNullOrEmpty(destUid)) {
                                await _hubContext.Clients.All.SendAsync("CallAnswered", !string.IsNullOrEmpty(destUid) ? destUid : uniqueid);
                            }

                            break;
                        }

                        case "varset":
                        {
                            var variable = Get(ev, "Variable");
                            if (variable == "MIXMONITOR_FILENAME")
                            {
                                var uniqueid = Get(ev, "Uniqueid");
                                var destUid = Get(ev, "DestUniqueid");
                                var filePath = Get(ev, "Value");
                                // https://192.168.1.150/index.php?menu=monitoring&action=display_record&id=1775648256.116234&namefile=rg-600-9123906718-20260408-160736-1775648256.116234.wav&rawmode=yes
                                // /var/spool/asterisk/monitor/2026/04/08/rg-600-9123906718-20260408-160736-1775648256.116234.wav


                                if ((!string.IsNullOrEmpty(uniqueid) || !string.IsNullOrEmpty(destUid)) && !string.IsNullOrEmpty(filePath))
                                {
                                    Console.WriteLine(filePath, uniqueid, destUid);
                                }
                            }

                            break;
                        }
                        /*case "blindtransfer":
                        case "localbridge":
                        {
                            var uniqueid = Get(ev, "Uniqueid");
                            //var linkedId = Get(ev, "LinkedId");
                            var linkedId = Get(ev, "TransfererUniqueid");

                            if (!string.IsNullOrEmpty(linkedId)) {
                                await _hubContext.Clients.All.SendAsync("CallEnded", linkedId);
                            }

                            break;
                        }*/
                    }
                }
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"[AMI] ارتباط قطع شد: {ex.Message}");
            return;
            //await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task<AppUser> GetUsers(string destCaller) {
        AppUser user = null;
        user = _users
            .FirstOrDefault(u => (u.InternalTels ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Contains(destCaller));
        return user;
    }

    private static Dictionary<string, Customer> _cusCache = new();
    private static DateTime _lastCacheTime = DateTime.MinValue;

    private string GetCallerText(string srcCaller) {
        if (string.IsNullOrEmpty(srcCaller)) return srcCaller;
        var key = srcCaller.Length > 7 ? srcCaller[^7..] : srcCaller;

        // هر 1 دقیقه کش اپدیت بشه
        if ((DateTime.Now - _lastCacheTime).TotalSeconds > 60 || _cusCache.Count == 0) {
            using var scope = serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CRMContextDb>();

            var list = db.Customers
                .Select(c => new { c.CompanyName, c.AgentName, c.Tel, c.Mobile })
                .ToList();
            var dict = new Dictionary<string, Customer>(list.Count * 2);

            foreach (var c in list) {
                if (!string.IsNullOrEmpty(c.Tel)) {
                    var k = c.Tel.Length > 7 ? c.Tel[^7..] : c.Tel;
                    dict.TryAdd(k, new Customer { CompanyName = c.CompanyName, AgentName = c.AgentName });
                }

                if (!string.IsNullOrEmpty(c.Mobile)) {
                    var k = c.Mobile.Length > 7 ? c.Mobile[^7..] : c.Mobile;
                    dict.TryAdd(k, new Customer { CompanyName = c.CompanyName, AgentName = c.AgentName });
                }
            }

            _cusCache = dict;
            _lastCacheTime = DateTime.Now;
        }

        return _cusCache.TryGetValue(key, out var cus)
            ? $"{cus.CompanyName} {cus.AgentName} ({srcCaller})"
            : srcCaller;

        /*using (var scope = serviceScopeFactory.CreateScope())
        {
            var db =  scope.ServiceProvider.GetRequiredService<CRMContextDb>();

            var srcCallerLast = srcCaller.Substring(srcCaller.Length - 7);
            var srcCus = db.Customers.FirstOrDefault(f => f.Tel.EndsWith(srcCallerLast) || f.Mobile.EndsWith(srcCallerLast));
            srcCallerText = srcCus == null ? srcCaller : $"{srcCus.CompanyName} {srcCus.AgentName} ({srcCaller})";
        }*/
    }


    private static IEnumerable<Dictionary<string, string>> ParseAmiResponse(string chunk) {
        chunk = chunk.Replace("\r\n", "\n");
        var frames = chunk.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        foreach (var frame in frames) {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var lines = frame.Split('\n');

            foreach (var line in lines) {
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