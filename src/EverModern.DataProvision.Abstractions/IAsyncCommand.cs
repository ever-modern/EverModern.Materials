namespace EverModern.DataProvision.Abstractions;

public interface IAsyncCommand<T>
{
    Task<int> DeleteAsync(CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(CancellationToken cancellationToken = default);
}