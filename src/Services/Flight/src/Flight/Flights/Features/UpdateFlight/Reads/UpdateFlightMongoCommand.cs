using System;

using BuildingBlocks.Core.Event;

using Flight.Flights.Models;

namespace Flight.Flights.Features.UpdateFlight.Reads;

public class UpdateFlightMongoCommand : InternalCommand
{
    public UpdateFlightMongoCommand(long id, string flightNumber, long aircraftId, DateTime departureDate,
        long departureAirportId,
        DateTime arriveDate, long arriveAirportId, decimal durationMinutes, DateTime flightDate, FlightStatus status,
        decimal price, bool isDeleted)
    {
        this.Id = id;
        this.FlightNumber = flightNumber;
        this.AircraftId = aircraftId;
        this.DepartureDate = departureDate;
        this.DepartureAirportId = departureAirportId;
        this.ArriveDate = arriveDate;
        this.ArriveAirportId = arriveAirportId;
        this.DurationMinutes = durationMinutes;
        this.FlightDate = flightDate;
        this.Status = status;
        this.Price = price;
        this.IsDeleted = isDeleted;
    }

    public string FlightNumber { get; }
    public long AircraftId { get; }
    public DateTime DepartureDate { get; }
    public long DepartureAirportId { get; }
    public DateTime ArriveDate { get; }
    public long ArriveAirportId { get; }
    public decimal DurationMinutes { get; }
    public DateTime FlightDate { get; }
    public FlightStatus Status { get; }
    public decimal Price { get; }
    public bool IsDeleted { get; }
}
