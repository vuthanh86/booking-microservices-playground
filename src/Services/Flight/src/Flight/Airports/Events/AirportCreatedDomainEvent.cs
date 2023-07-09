using BuildingBlocks.Core.Event;

namespace Flight.Airports.Events;

public record AirportCreatedDomainEvent
    (long Id, string Name, string Address, string Code, bool IsDeleted) : IDomainEvent;
