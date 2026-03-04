namespace EverModern.DataProvision.Abstractions;

/// <summary>
/// Extension methods for the Entity Framework queryable facade.
/// </summary>
public static class EntityFrameworkQueryableFacadeExtensions
{
    extension<T>(IQueryable<T> source)
    {
        /// <summary>
        /// Wraps the queryable in an Entity Framework-aware surface.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The queryable to wrap.</param>
        public IEntityFrameworkQueryable<T> AsEntityFrameworkQueryable()
            => new EntityFrameworkQueryableFacade<T>(source);

        /// <summary>
        /// Wraps the queryable in a read-only asynchronous surface.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The queryable to wrap.</param>
        public IReadOnlyAsyncQueryable<T> AsReadOnlyAsyncQueryable()
            => new EntityFrameworkQueryableFacade<T>(source);

        /// <summary>
        /// Wraps the queryable in an async-only surface.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The queryable to wrap.</param>
        public IAsyncOnlyQueryable<T> AsAsyncOnlyQueryable()
            => new EntityFrameworkQueryableFacade<T>(source);
    }
}
