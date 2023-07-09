using BuildingBlocks.Core.Event;
using BuildingBlocks.EventStoreDB.Events;
using BuildingBlocks.EventStoreDB.Serialization;

using EventStore.Client;

using JetBrains.Annotations;

namespace BuildingBlocks.EventStoreDB.Repository;

public interface IEventStoreDbRepository<T>
    where T : class, IAggregateEventSourcing<long>
{
    [ItemCanBeNull]
    Task<T> FindAsync(long id, CancellationToken cancellationToken);
    Task<ulong> AddAsync(T aggregate, CancellationToken cancellationToken);
    Task<ulong> UpdateAsync(T aggregate, long? expectedRevision = null, CancellationToken cancellationToken = default);
    Task<ulong> DeleteAsync(T aggregate, long? expectedRevision = null, CancellationToken cancellationToken = default);
}

public class EventStoreDbRepository<T> : IEventStoreDbRepository<T>
    where T : class, IAggregateEventSourcing<long>
{
    private readonly EventStoreClient _eventStore;

    public EventStoreDbRepository(EventStoreClient eventStore)
    {
        this._eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
    }

    [ItemCanBeNull]
    public Task<T> FindAsync(long id, CancellationToken cancellationToken)
    {
        return _eventStore.AggregateStream<T>(
                                             id,
                                             cancellationToken
                                            );
    }

    public async Task<ulong> AddAsync(T aggregate, CancellationToken cancellationToken = default)
    {
        IWriteResult result = await _eventStore.AppendToStreamAsync(
                                                                   StreamNameMapper.ToStreamId<T>(aggregate.Id),
                                                                   StreamState.NoStream,
                                                                   GetEventsToStore(aggregate),
                                                                   cancellationToken: cancellationToken
                                                                  );
        return result.NextExpectedStreamRevision;
    }

    public async Task<ulong> UpdateAsync(T aggregate, long? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        long nextVersion = expectedRevision ?? aggregate.Version;

        IWriteResult result = await _eventStore.AppendToStreamAsync(
                                                                   StreamNameMapper.ToStreamId<T>(aggregate.Id),
                                                                   (ulong)nextVersion,
                                                                   GetEventsToStore(aggregate),
                                                                   cancellationToken: cancellationToken
                                                                  );
        return result.NextExpectedStreamRevision;
    }

    public Task<ulong> DeleteAsync(T aggregate, long? expectedRevision = null, CancellationToken cancellationToken = default)
    {
        return UpdateAsync(aggregate, expectedRevision, cancellationToken);
    }

    private static IEnumerable<EventData> GetEventsToStore(T aggregate)
    {
        IEvent[] events = aggregate.ClearDomainEvents();

        return events
            .Select(EventStoreDbSerializer.ToJsonEventData);
    }
}
