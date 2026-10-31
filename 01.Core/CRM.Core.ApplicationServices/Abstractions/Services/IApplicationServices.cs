using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.LogSystems.Entities;

namespace CRM.Core.ApplicationServices.Abstractions.Services;
public interface IApplicationAudit
{
    Task LogAsync<T>(T original, T? current, LogActionEnum action, AppUser user, CancellationToken cancellationToken, List<string>? collections = null, string? comment = null);
}

public interface IApplicationDates
{
    DateTime? ParsePersian(string? value);
    string PersianNow();
    string PersianToday();
    string Elapsed(DateTime value);
}
