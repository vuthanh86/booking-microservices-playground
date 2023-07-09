using BuildingBlocks.Core.CQRS;

using Passenger.Passengers.Dtos;

namespace Passenger.Passengers.Features.GetPassengerById;

public record GetPassengerQueryById(long Id) : IQuery<PassengerResponseDto>;
