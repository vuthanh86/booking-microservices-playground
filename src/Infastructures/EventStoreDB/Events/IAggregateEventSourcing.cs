using BuildingBlocks.Core.Event;
using BuildingBlocks.Core.Model;

namespace BuildingBlocks.EventStoreDB.Events;

public interface IAggregateEventSourcing : IProjection, IEntity
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    long Version { get; }
    IEvent[] ClearDomainEvents();
}

public interface IAggregateEventSourcing<out T> : IAggregateEventSourcing
{
    T Id { get; }
}
