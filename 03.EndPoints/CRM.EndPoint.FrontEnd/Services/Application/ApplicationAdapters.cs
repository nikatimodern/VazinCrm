using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.EndPoint.FrontEnd.Services.LogSystem;

namespace CRM.EndPoint.FrontEnd.Services.Application;
public sealed class ApplicationAudit(LogSystemService log) : IApplicationAudit
{
    public async Task LogAsync<T>(T original, T current, LogActionEnum action, AppUser user, CancellationToken ct, List<string> collections = null, string comment = null)
    {
        ct.ThrowIfCancellationRequested();
        await log.LogChange(original, current, action, user, collections, comment);
    }
}

public sealed class ApplicationDates(DateTimeServices dates) : IApplicationDates
{
    public DateTime? ParsePersian(string value) => dates.GetDateMiladi(value);
    public string PersianNow() => dates.GetDateTimePersian();
    public string PersianToday() => dates.GetDatePersian();
    public string Elapsed(DateTime value) => dates.ElapsTime(value);
}
