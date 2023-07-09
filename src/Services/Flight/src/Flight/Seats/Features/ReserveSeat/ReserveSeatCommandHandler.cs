using System.Threading;
using System.Threading.Tasks;

using Ardalis.GuardClauses;

using Flight.Data;
using Flight.Seats.Dtos;
using Flight.Seats.Exceptions;
using Flight.Seats.Models;

using MapsterMapper;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Flight.Seats.Features.ReserveSeat;

public class ReserveSeatCommandHandler : IRequestHandler<ReserveSeatCommand, SeatResponseDto>
{
    private readonly FlightDbContext _flightDbContext;
    private readonly IMapper _mapper;

    public ReserveSeatCommandHandler(IMapper mapper, FlightDbContext flightDbContext)
    {
        _mapper = mapper;
        _flightDbContext = flightDbContext;
    }

    public async Task<SeatResponseDto> Handle(ReserveSeatCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command, nameof(command));

        Seat seat
            = await
                  _flightDbContext.Seats
                      .SingleOrDefaultAsync(x => x.SeatNumber == command.SeatNumber && x.FlightId == command.FlightId,
                                            cancellationToken);

        if (seat is null)
            throw new SeatNumberIncorrectException();

        Seat reserveSeat = await seat.ReserveSeat(seat);

        EntityEntry<Seat> updatedSeat = _flightDbContext.Seats.Update(reserveSeat);

        return _mapper.Map<SeatResponseDto>(updatedSeat.Entity);
    }
}
