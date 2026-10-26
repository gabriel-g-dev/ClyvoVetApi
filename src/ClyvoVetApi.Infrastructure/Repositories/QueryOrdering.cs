using System.Linq.Expressions;

namespace ClyvoVetApi.Repositories;

internal static class QueryOrdering
{
    public static IOrderedQueryable<T> By<T, TKey>(
        IQueryable<T> query, Expression<Func<T, TKey>> field, bool descending) =>
        descending ? query.OrderByDescending(field) : query.OrderBy(field);
}
