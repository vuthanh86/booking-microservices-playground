using System.Text;

using BuildingBlocks.EventStoreDB.Events;

using EventStore.Client;

using JetBrains.Annotations;

using Newtonsoft.Json;

namespace BuildingBlocks.EventStoreDB.Serialization;

public static class EventStoreDbSerializer
{
    private static readonly JsonSerializerSettings _serializerSettings =
        new JsonSerializerSettings().WithNonDefaultConstructorContractResolver();

    [CanBeNull]
    public static T Deserialize<T>(this ResolvedEvent resolvedEvent)
        where T : class
    {
        return Deserialize(resolvedEvent) as T;
    }

    [CanBeNull]
    public static object Deserialize(this ResolvedEvent resolvedEvent)
    {
        // get type
        Type eventType = EventTypeMapper.ToType(resolvedEvent.Event.EventType);

        if (eventType == null)
            return null;

        // deserialize event
        return JsonConvert.DeserializeObject(
                                             Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span),
                                             eventType,
                                             _serializerSettings
                                            )!;
    }

    public static EventData ToJsonEventData(this object @event)
    {
        return new EventData(
                             Uuid.NewUuid(),
                             EventTypeMapper.ToName(@event.GetType()),
                             Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event)),
                             Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { }))
                            );
    }
}
