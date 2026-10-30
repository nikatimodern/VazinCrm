using CRM.Core.Domain.Reports.Models;
using CRM.EndPoint.FrontEnd.Services;
using System;
namespace CRM.EndPoint.FrontEnd.Models;
public static class ReportFilterMapping
{
    public static ReportSearch ToReportSearch(this ReportFilterVModel model)
    {
        var filter = model.filt;
        var dates = filter is null ? null : new DateTimeServices();
        return new(model.serVal, model._type, model.Count, model.ResultCallId, model.FinalResultCallId, model.ReasonCancelId,
            filter?.CusFilterChd ?? false, filter?.customerFilter,
            filter?.SelFilterChd ?? false, filter?.selFilter.ToString(),
            filter?.DateFilterChk ?? false,
            filter is null ? null : dates.GetDateMiladi(filter.fromDate) ?? dates.GetDateMiladi("1000/01/01"),
            filter is null ? null : (dates.GetDateMiladi(filter.toDate) ?? DateTime.Today).Date.AddDays(1).AddTicks(-1));
    }
}
