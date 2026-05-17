using EverModern.DataProvision.Abstractions;

namespace EverModern.QueryKit;

public static class QueryExtensions
{
    extension<T>(IQueryable<T> source)
    {
        public AsyncQueryableFacade<T> AsAsyncReadOnlyQueryable() => new(source);

        public IAsyncMaterializable<T> MaterializeAsync(IQueryable<T> query) =>
            query as IAsyncMaterializable<T>;
    }

    extension<T>(IReadOnlyAsyncQueryable<T> asyncQueryable)
    {
        public IQueryable<T> Query(IReadOnlyAsyncQueryable<T> asyncQuery) =>
            asyncQuery as IQueryable<T>;
    }
}
