namespace EverModern.DataProvision.Abstractions;

public static class QueryableExtensions
{
    extension<T>(IQueryable<T> source)
    {
        /// <summary>
        /// Wraps the queryable in a read-only synchronous surface.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The queryable to wrap.</param>
        public IReadOnlyQueryable<T> AsReadOnlyQueryable()
            => new QueryableLimittingFacade<T>(source);

        // Async queryable wrappers are available in the DataProvision project.
    }
}
