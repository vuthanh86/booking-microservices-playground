using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ardalis.GuardClauses;

using Flight.Data;
using Flight.Seats.Dtos;
using Flight.Seats.Exceptions;
using Flight.Seats.Models.Reads;

using MapsterMapper;

using MediatR;

using MongoDB.Driver;

namespace Flight.Seats.Features.GetAvailableSeats;

public class GetAvailableSeatsQueryHandler : IRequestHandler<GetAvailableSeatsQuery, IEnumerable<SeatResponseDto>>
{
    private readonly FlightReadDbContext _flightReadDbContext;
    private readonly IMapper _mapper;

    public GetAvailableSeatsQueryHandler(IMapper mapper, FlightReadDbContext flightReadDbContext)
    {
        _mapper = mapper;
        _flightReadDbContext = flightReadDbContext;
    }


    public async Task<IEnumerable<SeatResponseDto>> Handle(GetAvailableSeatsQuery query,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(query, nameof(query));

        IEnumerable<SeatReadModel> seats
            = (await _flightReadDbContext.Seat.AsQueryable().ToListAsync(cancellationToken))
            .Where(x => x.FlightId == query.FlightId);

        if (!seats.Any())
            throw new AllSeatsFullException();

        return _mapper.Map<List<SeatResponseDto>>(seats);
    }
}
