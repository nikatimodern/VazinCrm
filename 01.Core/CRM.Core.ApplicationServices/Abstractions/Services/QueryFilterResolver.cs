using CRM.Core.Domain.Querying;

namespace CRM.Core.ApplicationServices.Abstractions.Services;
public static class QueryFilterResolver
{
    public static QueryFilter Resolve(QueryFilterInput? input, IApplicationDates dates)
    {
        input ??= new(null, 0, 0, null, false, false, null, false, null, null, false, null);
        var from = input.FilterDate ? dates.ParsePersian(input.PersianFrom) ?? dates.ParsePersian("1000/01/01") : null;
        var to = input.FilterDate ? (dates.ParsePersian(input.PersianTo) ?? DateTime.Today).Date.AddDays(1).AddTicks(-1) : (DateTime? )null;
        return new(input.Search, input.Type, input.Count, input.EntityId, input.HasFilter, input.FilterCustomer, input.CustomerId, input.FilterDate, from, to, input.FilterUser, input.UserId?.ToString() ?? "");
    }
}
