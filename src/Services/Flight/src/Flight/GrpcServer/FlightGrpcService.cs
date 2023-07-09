using System.Collections.Generic;

using BuildingBlocks.Contracts.Grpc;

using Flight.Flights.Features.GetFlightById;
using Flight.Seats.Features.GetAvailableSeats;
using Flight.Seats.Features.ReserveSeat;

using MagicOnion;
using MagicOnion.Server;

using Mapster;

using MediatR;

namespace Flight.GrpcServer;

public class FlightGrpcService : ServiceBase<IFlightGrpcService>, IFlightGrpcService
{
    private readonly IMediator _mediator;

    public FlightGrpcService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async UnaryResult<FlightResponseDto> GetById(long id)
    {
        Flights.Dtos.FlightResponseDto result = await _mediator.Send(new GetFlightByIdQuery(id));
        return result.Adapt<FlightResponseDto>();
    }

    public async UnaryResult<IEnumerable<SeatResponseDto>> GetAvailableSeats(long flightId)
    {
        IEnumerable<Seats.Dtos.SeatResponseDto> result = await _mediator.Send(new GetAvailableSeatsQuery(flightId));
        return result.Adapt<IEnumerable<SeatResponseDto>>();
    }

    public async UnaryResult<SeatResponseDto> ReserveSeat(ReserveSeatRequestDto request)
    {
        Seats.Dtos.SeatResponseDto result
            = await _mediator.Send(new ReserveSeatCommand(request.FlightId, request.SeatNumber));
        return result.Adapt<SeatResponseDto>();
    }
}
