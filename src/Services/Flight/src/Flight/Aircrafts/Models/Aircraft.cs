using BuildingBlocks.Core.Model;

using Flight.Aircrafts.Events;

namespace Flight.Aircrafts.Models;

public class Aircraft : Aggregate<long>
{
    public string Name { get; private set; }
    public string Model { get; private set; }
    public int ManufacturingYear { get; private set; }

    public static Aircraft Create(long id, string name, string model, int manufacturingYear, bool isDeleted = false)
    {
        Aircraft aircraft = new Aircraft { Id = id, Name = name, Model = model, ManufacturingYear = manufacturingYear };

        AircraftCreatedDomainEvent @event = new AircraftCreatedDomainEvent(
                                                                           aircraft.Id,
                                                                           aircraft.Name,
                                                                           aircraft.Model,
                                                                           aircraft.ManufacturingYear,
                                                                           isDeleted);

        aircraft.AddDomainEvent(@event);

        return aircraft;
    }
}
