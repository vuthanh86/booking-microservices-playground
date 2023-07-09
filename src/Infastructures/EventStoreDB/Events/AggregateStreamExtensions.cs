using BuildingBlocks.EventStoreDB.Serialization;

using EventStore.Client;

using JetBrains.Annotations;

namespace BuildingBlocks.EventStoreDB.Events;

public static class AggregateStreamExtensions
{
    [ItemCanBeNull]
    public static async Task<T> AggregateStream<T>(
        this EventStoreClient eventStore,
        long id,
        CancellationToken cancellationToken,
        ulong? fromVersion = null
    ) where T : class, IProjection
    {
        EventStoreClient.ReadStreamResult readResult = eventStore.ReadStreamAsync(
             Direction.Forwards,
             StreamNameMapper.ToStreamId<T>(id),
             fromVersion ?? StreamPosition.Start,
             cancellationToken: cancellationToken
            );

        // TODO: consider adding extension method for the aggregation and deserialisation
        T aggregate = (T)Activator.CreateInstance(typeof(T), true)!;

        if (await readResult.ReadState == ReadState.StreamNotFound)
            return null;

        await foreach (ResolvedEvent @event in readResult)
        {
            object eventData = @event.Deserialize();

            aggregate.When(eventData!);
        }

        return aggregate;
    }
}
