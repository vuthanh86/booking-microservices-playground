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

namespace Flight.Seats.Features.CreateSeat;

public class CreateSeatCommandHandler : IRequestHandler<CreateSeatCommand, SeatResponseDto>
{
    private readonly FlightDbContext _flightDbContext;
    private readonly IMapper _mapper;

    public CreateSeatCommandHandler(IMapper mapper, FlightDbContext flightDbContext)
    {
        _mapper = mapper;
        _flightDbContext = flightDbContext;
    }

    public async Task<SeatResponseDto> Handle(CreateSeatCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command, nameof(command));

        Seat seat = await _flightDbContext.Seats.SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (seat is not null)
            throw new SeatAlreadyExistException();

        Seat seatEntity = Seat.Create(command.Id, command.SeatNumber, command.Type, command.Class, command.FlightId);

        EntityEntry<Seat> newSeat = await _flightDbContext.Seats.AddAsync(seatEntity, cancellationToken);

        return _mapper.Map<SeatResponseDto>(newSeat.Entity);
    }
}
