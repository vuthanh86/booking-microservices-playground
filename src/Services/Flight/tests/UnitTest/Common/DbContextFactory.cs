using System;
using System.Collections.Generic;

using Flight.Data;
using Flight.Flights.Models;
using Flight.Seats.Models;

using Microsoft.EntityFrameworkCore;

namespace Unit.Test.Common;

public static class DbContextFactory
{
    public static FlightDbContext Create()
    {
        DbContextOptions<FlightDbContext> options = new DbContextOptionsBuilder<FlightDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

        FlightDbContext context = new FlightDbContext(options, null);

        // Seed our data
        FlightDataSeeder(context);

        return context;
    }

    private static void FlightDataSeeder(FlightDbContext context)
    {
        List<global::Flight.Airports.Models.Airport> airports = new List<global::Flight.Airports.Models.Airport>
        {
            global::Flight.Airports.Models.Airport.Create(1, "Lisbon International Airport", "LIS", "12988"),
            global::Flight.Airports.Models.Airport.Create(2, "Sao Paulo International Airport", "BRZ", "11200")
        };

        context.Airports.AddRange(airports);

        List<global::Flight.Aircrafts.Models.Aircraft> aircrafts = new List<global::Flight.Aircrafts.Models.Aircraft>
        {
            global::Flight.Aircrafts.Models.Aircraft.Create(1, "Boeing 737", "B737", 2005),
            global::Flight.Aircrafts.Models.Aircraft.Create(2, "Airbus 300", "A300", 2000),
            global::Flight.Aircrafts.Models.Aircraft.Create(3, "Airbus 320", "A320", 2003)
        };

        context.Aircraft.AddRange(aircrafts);

        List<global::Flight.Flights.Models.Flight> flights = new List<global::Flight.Flights.Models.Flight>
        {
            global::Flight.Flights.Models.Flight.Create(1, "BD467", 1, 1, new DateTime(2022, 1, 31, 12, 0, 0),
                                                        new DateTime(2022, 1, 31, 14, 0, 0),
                                                        2, 120m,
                                                        new DateTime(2022, 1, 31), FlightStatus.Completed,
                                                        8000)
        };
        context.Flights.AddRange(flights);

        List<global::Flight.Seats.Models.Seat> seats = new List<global::Flight.Seats.Models.Seat>
        {
            global::Flight.Seats.Models.Seat.Create(1, "12A", SeatType.Window, SeatClass.Economy, 1),
            global::Flight.Seats.Models.Seat.Create(2, "12B", SeatType.Window, SeatClass.Economy, 1),
            global::Flight.Seats.Models.Seat.Create(3, "12C", SeatType.Middle, SeatClass.Economy, 1),
            global::Flight.Seats.Models.Seat.Create(4, "12D", SeatType.Middle, SeatClass.Economy, 1),
            global::Flight.Seats.Models.Seat.Create(5, "12E", SeatType.Aisle, SeatClass.Economy, 1),
            global::Flight.Seats.Models.Seat.Create(6, "12F", SeatType.Aisle, SeatClass.Economy, 1)
        };

        context.Seats.AddRange(seats);

        context.SaveChanges();
    }

    public static void Destroy(FlightDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }
}
