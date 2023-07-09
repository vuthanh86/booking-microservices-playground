using BuildingBlocks.Core.Event;
using BuildingBlocks.EventStoreDB.Serialization;

using EventStore.Client;

namespace BuildingBlocks.EventStoreDB.Subscriptions;

public record CheckpointStored(string SubscriptionId, ulong? Position, DateTime CheckpointedAt) : IEvent;

public class EventStoreDbSubscriptionCheckpointRepository : ISubscriptionCheckpointRepository
{
    private readonly EventStoreClient _eventStoreClient;

    public EventStoreDbSubscriptionCheckpointRepository(
        EventStoreClient eventStoreClient)
    {
        this._eventStoreClient = eventStoreClient ?? throw new ArgumentNullException(nameof(eventStoreClient));
    }

    public async ValueTask<ulong?> Load(string subscriptionId, CancellationToken ct)
    {
        string streamName = GetCheckpointStreamName(subscriptionId);

        EventStoreClient.ReadStreamResult result = _eventStoreClient.ReadStreamAsync(Direction.Backwards, streamName,
         StreamPosition.End, 1,
         cancellationToken: ct);

        if (await result.ReadState == ReadState.StreamNotFound)
            return null;

        ResolvedEvent? @event = await result.FirstOrDefaultAsync(ct);

        return @event?.Deserialize<CheckpointStored>()?.Position;
    }

    public async ValueTask Store(string subscriptionId, ulong position, CancellationToken ct)
    {
        CheckpointStored @event = new CheckpointStored(subscriptionId, position, DateTime.UtcNow);
        EventData[] eventToAppend = new[] { @event.ToJsonEventData() };
        string streamName = GetCheckpointStreamName(subscriptionId);

        try
        {
            // store new checkpoint expecting stream to exist
            await _eventStoreClient.AppendToStreamAsync(
                                                       streamName,
                                                       StreamState.StreamExists,
                                                       eventToAppend,
                                                       cancellationToken: ct
                                                      );
        }
        catch (WrongExpectedVersionException)
        {
            // WrongExpectedVersionException means that stream did not exist
            // Set the checkpoint stream to have at most 1 event
            // using stream metadata $maxCount property
            await _eventStoreClient.SetStreamMetadataAsync(
                                                          streamName,
                                                          StreamState.NoStream,
                                                          new StreamMetadata(1),
                                                          cancellationToken: ct
                                                         );

            // append event again expecting stream to not exist
            await _eventStoreClient.AppendToStreamAsync(
                                                       streamName,
                                                       StreamState.NoStream,
                                                       eventToAppend,
                                                       cancellationToken: ct
                                                      );
        }
    }

    private static string GetCheckpointStreamName(string subscriptionId)
    {
        return $"checkpoint_{subscriptionId}";
    }
}
