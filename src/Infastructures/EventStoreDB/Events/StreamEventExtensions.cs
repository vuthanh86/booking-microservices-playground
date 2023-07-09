using BuildingBlocks.EventStoreDB.Serialization;

using EventStore.Client;

using JetBrains.Annotations;

namespace BuildingBlocks.EventStoreDB.Events;

public static class StreamEventExtensions
{
    [CanBeNull]
    public static StreamEvent ToStreamEvent(this ResolvedEvent resolvedEvent)
    {
        object eventData = resolvedEvent.Deserialize();
        if (eventData == null)
            return null;

        EventMetadata metaData = new EventMetadata(resolvedEvent.Event.EventNumber.ToUInt64(),
                                                   resolvedEvent.Event.Position.CommitPosition);
        Type type = typeof(StreamEvent<>).MakeGenericType(eventData.GetType());
        return (StreamEvent)Activator.CreateInstance(type, eventData, metaData)!;
    }
}
