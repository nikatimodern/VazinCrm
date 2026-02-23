using System;
using System.Linq;
using System.Linq.Expressions;
using CRM.EndPoint.FrontEnd.Models.ViewModel;

namespace CRM.EndPoint.FrontEnd.Services.QueryExtensions;

public static class QueryFilterExtensions {
    public static IQueryable<T> ApplyFilter<T>(
        this IQueryable<T> query,
        filterVModel model,
        Expression<Func<T, Guid?>> customerSelector = null,
        Expression<Func<T, DateTime?>> dateSelector = null,
        Expression<Func<T, string>> userSelector = null)
    {
        var dtService = new DateTimeServices();
        var fd = dtService.GetDateMiladi(model.filt.fromDate) ?? dtService.GetDateMiladi("1000/01/01");
        var td = (dtService.GetDateMiladi(model.filt.toDate) ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

        if (model.filt.CusFilterChd && customerSelector != null)
        {
            var left = customerSelector.Body;
            var constVal = Expression.Constant((Guid?)model.filt.customerFilter, typeof(Guid?));

            var equal = Expression.Equal(left, constVal);

            query = query.Where(Expression.Lambda<Func<T, bool>>(equal, customerSelector.Parameters));
        }


        /*if (model.filt.DateFilterChk && dateSelector != null)
        {
            var greater = Expression.GreaterThanOrEqual(dateSelector.Body, Expression.Constant((DateTime?)fd));
            var less = Expression.LessThanOrEqual(dateSelector.Body, Expression.Constant((DateTime?)td));

            query = query.Where(Expression.Lambda<Func<T, bool>>(Expression.AndAlso(greater, less), dateSelector.Parameters));
        }*/
        if (model.filt.DateFilterChk && dateSelector != null)
        {
            // dateSelector.Body ممکنه DateTime? باشه
            var left = dateSelector.Body;

            // تبدیل constant به DateTime? (nullable)
            var startConst = Expression.Constant((DateTime?)fd, typeof(DateTime?));
            var endConst = Expression.Constant((DateTime?)td, typeof(DateTime?));

            // Nullable aware comparison
            var greater = Expression.GreaterThanOrEqual(left, startConst);
            var less = Expression.LessThanOrEqual(left, endConst);

            var body = Expression.AndAlso(greater, less);

            query = query.Where(Expression.Lambda<Func<T, bool>>(body, dateSelector.Parameters));
        }

        if (model.filt.SelFilterChd && userSelector != null)
            query = query.Where(Expression.Lambda<Func<T, bool>>(
                Expression.Equal(userSelector.Body, Expression.Constant(model.filt.selFilter.ToString())),
                userSelector.Parameters));

        return query;
    }
}