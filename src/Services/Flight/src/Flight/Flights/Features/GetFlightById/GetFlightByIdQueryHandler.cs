using System.Threading;
using System.Threading.Tasks;

using Ardalis.GuardClauses;

using BuildingBlocks.Core.CQRS;

using Flight.Data;
using Flight.Flights.Dtos;
using Flight.Flights.Exceptions;
using Flight.Flights.Models.Reads;

using MapsterMapper;

using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Flight.Flights.Features.GetFlightById;

public class GetFlightByIdQueryHandler : IQueryHandler<GetFlightByIdQuery, FlightResponseDto>
{
    private readonly FlightReadDbContext _flightReadDbContext;
    private readonly IMapper _mapper;

    public GetFlightByIdQueryHandler(IMapper mapper, FlightReadDbContext flightReadDbContext)
    {
        _mapper = mapper;
        _flightReadDbContext = flightReadDbContext;
    }

    public async Task<FlightResponseDto> Handle(GetFlightByIdQuery query, CancellationToken cancellationToken)
    {
        Guard.Against.Null(query, nameof(query));

        FlightReadModel flight =
            await _flightReadDbContext.Flight.AsQueryable()
                .SingleOrDefaultAsync(x => x.FlightId == query.Id, cancellationToken);

        if (flight is null)
            throw new FlightNotFountException();

        return _mapper.Map<FlightResponseDto>(flight);
    }
}
