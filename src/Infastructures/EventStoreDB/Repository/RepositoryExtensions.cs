using BuildingBlocks.EventStoreDB.Events;
using BuildingBlocks.Exception;

namespace BuildingBlocks.EventStoreDB.Repository;

public static class RepositoryExtensions
{
    public static async Task<T> GetAsync<T>(
        this IEventStoreDbRepository<T> repository,
        long id,
        CancellationToken cancellationToken
    )
        where T : class, IAggregateEventSourcing<long>
    {
        T entity = await repository.FindAsync(id, cancellationToken);

        return entity ?? throw AggregateNotFoundException.For<T>(id);
    }

    public static async Task<ulong> GetAndUpdate<T>(
        this IEventStoreDbRepository<T> repository,
        long id,
        Action<T> action,
        long? expectedVersion = null,
        CancellationToken cancellationToken = default
    )
        where T : class, IAggregateEventSourcing<long>
    {
        T entity = await repository.GetAsync(id, cancellationToken);

        action(entity);

        return await repository.UpdateAsync(entity, expectedVersion, cancellationToken);
    }
}
