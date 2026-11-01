using CRM.Core.Domain.Querying;

namespace CRM.EndPoint.FrontEnd.Models;
public static class ApplicationFilterMapping
{
    public static QueryFilterInput ToApplicationFilter(this FilterVModel model) => new(model?.serVal, model?._type ?? 0, model?.Count ?? 0, model?.EntityId, model?.filt != null, model?.filt?.CusFilterChd == true, model?.filt?.customerFilter, model?.filt?.DateFilterChk == true, model?.filt?.fromDate, model?.filt?.toDate, model?.filt?.SelFilterChd == true, model?.filt?.selFilter);
}
