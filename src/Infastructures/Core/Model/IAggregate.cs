﻿using BuildingBlocks.Core.Event;

namespace BuildingBlocks.Core.Model;

public interface IAggregate : IEntity
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    long Version { get; set; }
    IEvent[] ClearDomainEvents();
}

public interface IAggregate<out T> : IAggregate
{
    T Id { get; }
}
