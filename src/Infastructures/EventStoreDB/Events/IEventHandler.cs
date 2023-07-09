using BuildingBlocks.Core.Event;

using MediatR;

namespace BuildingBlocks.EventStoreDB.Events;

public interface IEventHandler<in TEvent> : INotificationHandler<TEvent>
    where TEvent : IEvent
{ }
