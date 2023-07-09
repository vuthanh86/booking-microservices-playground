using System.Collections.Generic;

using BuildingBlocks.Core.CQRS;

using Flight.Seats.Dtos;

namespace Flight.Seats.Features.GetAvailableSeats;

public record GetAvailableSeatsQuery(long FlightId) : IQuery<IEnumerable<SeatResponseDto>>;
