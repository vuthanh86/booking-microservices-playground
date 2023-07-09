using BuildingBlocks.Core.Model;

using Flight.Airports.Events;

namespace Flight.Airports.Models;

public class Airport : Aggregate<long>
{
    public string Name { get; private set; }
    public string Address { get; private set; }
    public string Code { get; private set; }

    public static Airport Create(long id, string name, string address, string code, bool isDeleted = false)
    {
        Airport airport = new Airport { Id = id, Name = name, Address = address, Code = code };

        AirportCreatedDomainEvent @event = new AirportCreatedDomainEvent(
                                                                         airport.Id,
                                                                         airport.Name,
                                                                         airport.Address,
                                                                         airport.Code,
                                                                         isDeleted);

        airport.AddDomainEvent(@event);

        return airport;
    }
}
