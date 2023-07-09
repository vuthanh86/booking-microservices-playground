using System.Threading;
using System.Threading.Tasks;

using Ardalis.GuardClauses;

using BuildingBlocks.Core.CQRS;

using Flight.Data;
using Flight.Flights.Dtos;
using Flight.Flights.Exceptions;

using MapsterMapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Flight.Flights.Features.UpdateFlight;

public class UpdateFlightCommandHandler : ICommandHandler<UpdateFlightCommand, FlightResponseDto>
{
    private readonly FlightDbContext _flightDbContext;
    private readonly IMapper _mapper;

    public UpdateFlightCommandHandler(IMapper mapper, FlightDbContext flightDbContext)
    {
        _mapper = mapper;
        _flightDbContext = flightDbContext;
    }

    public async Task<FlightResponseDto> Handle(UpdateFlightCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command, nameof(command));

        Models.Flight flight = await _flightDbContext.Flights.SingleOrDefaultAsync(x => x.Id == command.Id,
                                cancellationToken);

        if (flight is null)
            throw new FlightNotFountException();

        flight.Update(command.Id, command.FlightNumber, command.AircraftId, command.DepartureAirportId,
                      command.DepartureDate,
                      command.ArriveDate, command.ArriveAirportId, command.DurationMinutes, command.FlightDate,
                      command.Status, command.Price, command.IsDeleted);

        EntityEntry<Models.Flight> updateFlight = _flightDbContext.Flights.Update(flight);

        return _mapper.Map<FlightResponseDto>(updateFlight.Entity);
    }
}
