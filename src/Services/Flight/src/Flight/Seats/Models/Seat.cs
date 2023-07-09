using System;
using System.Threading.Tasks;

using BuildingBlocks.Core.Model;

using Flight.Seats.Events;

namespace Flight.Seats.Models;

public class Seat : Aggregate<long>
{
    public string SeatNumber { get; private set; }
    public SeatType Type { get; private set; }
    public SeatClass Class { get; private set; }
    public long FlightId { get; private set; }

    public static Seat Create(long id, string seatNumber, SeatType type, SeatClass @class, long flightId,
        bool isDeleted = false)
    {
        Seat seat = new Seat
        {
            Id = id,
            Class = @class,
            Type = type,
            SeatNumber = seatNumber,
            FlightId = flightId,
            IsDeleted = isDeleted
        };

        SeatCreatedDomainEvent @event = new SeatCreatedDomainEvent(
                                                                   seat.Id,
                                                                   seat.SeatNumber,
                                                                   seat.Type,
                                                                   seat.Class,
                                                                   seat.FlightId,
                                                                   isDeleted);

        seat.AddDomainEvent(@event);

        return seat;
    }

    public Task<Seat> ReserveSeat(Seat seat)
    {
        seat.IsDeleted = true;
        seat.LastModified = DateTime.Now;

        SeatReservedDomainEvent @event = new SeatReservedDomainEvent(
                                                                     seat.Id,
                                                                     seat.SeatNumber,
                                                                     seat.Type,
                                                                     seat.Class,
                                                                     seat.FlightId,
                                                                     seat.IsDeleted);

        seat.AddDomainEvent(@event);

        return Task.FromResult(this);
    }
}
