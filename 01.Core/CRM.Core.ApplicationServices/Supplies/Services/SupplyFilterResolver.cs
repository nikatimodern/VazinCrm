using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Services;
public static class SupplyFilterResolver
{
    public static SupplyTableRequest Resolve(SupplyTableRequest request, IApplicationDates dates)
    {
        if (!request.RawDateInput || !request.DateFilterEnabled)
            return request;
        return request with
        {
            FromDate = dates.ParsePersian(request.PersianFrom) ?? dates.ParsePersian("1000/01/01"),
            ToDate = (dates.ParsePersian(request.PersianTo) ?? DateTime.Today).Date.AddDays(1).AddTicks(-1)
        };
    }
}
