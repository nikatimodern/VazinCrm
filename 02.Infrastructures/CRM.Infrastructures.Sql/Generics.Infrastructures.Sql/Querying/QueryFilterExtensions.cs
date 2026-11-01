using System;
using System.Linq;
using System.Linq.Expressions;
using CRM.Core.Domain.Querying;

namespace CRM.Infrastructures.Sql.Querying;
public static class QueryFilterExtensions
{
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, QueryFilter filter, Expression<Func<T, Guid?>> customer = null, Expression<Func<T, DateTime?>> date = null, Expression<Func<T, string>> user = null)
    {
        if (!filter.HasFilter)
            return query;
        if (filter.FilterCustomer && customer != null)
            query = query.Where(Expression.Lambda<Func<T, bool>>(Expression.Equal(customer.Body, Expression.Constant(filter.CustomerId, typeof(Guid? ))), customer.Parameters));
        if (filter.FilterDate && date != null)
        {
            var start = Expression.GreaterThanOrEqual(date.Body, Expression.Constant(filter.From, typeof(DateTime? )));
            var end = Expression.LessThanOrEqual(date.Body, Expression.Constant(filter.To, typeof(DateTime? )));
            query = query.Where(Expression.Lambda<Func<T, bool>>(Expression.AndAlso(start, end), date.Parameters));
        }

        if (filter.FilterUser && user != null)
            query = query.Where(Expression.Lambda<Func<T, bool>>(Expression.Equal(user.Body, Expression.Constant(filter.UserId)), user.Parameters));
        return query;
    }
}
