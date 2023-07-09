using BuildingBlocks.Core.Event;

namespace BuildingBlocks.EventStoreDB.Events;

public record EventMetadata(
    ulong StreamRevision,
    ulong LogPosition
);

public class StreamEvent : IEvent
{
    public StreamEvent(object data, EventMetadata metadata)
    {
        Data = data;
        Metadata = metadata;
    }

    public object Data { get; }
    public EventMetadata Metadata { get; }
}

public class StreamEvent<T> : StreamEvent where T : notnull
{
    public StreamEvent(T data, EventMetadata metadata) : base(data, metadata)
    { }

    public new T Data => (T)base.Data;
}
