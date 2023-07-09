using Google.Protobuf;

using JetBrains.Annotations;

namespace BuildingBlocks.Core.Event;

public class MessageEnvelope
{
    public MessageEnvelope([CanBeNull] object message, [CanBeNull] IDictionary<string, object?> headers = null)
    {
        Message = message;
        Headers = headers ?? new Dictionary<string, object?>();
    }

    [CanBeNull] public object Message { get; init; }
    public IDictionary<string, object?> Headers { get; init; }
}

public class MessageEnvelope<TMessage> : MessageEnvelope
    where TMessage : class, IMessage
{
    public MessageEnvelope(TMessage message, IDictionary<string, object?> header) : base(message, header)
    {
        Message = message;
    }

    [CanBeNull] public new TMessage Message { get; }
}
