using Ardalis.GuardClauses;

using BuildingBlocks.Core.CQRS;

using MapsterMapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using Passenger.Data;
using Passenger.Passengers.Dtos;
using Passenger.Passengers.Exceptions;

namespace Passenger.Passengers.Features.CompleteRegisterPassenger;

public class
    CompleteRegisterPassengerCommandHandler : ICommandHandler<CompleteRegisterPassengerCommand, PassengerResponseDto>
{
    private readonly IMapper _mapper;
    private readonly PassengerDbContext _passengerDbContext;

    public CompleteRegisterPassengerCommandHandler(IMapper mapper, PassengerDbContext passengerDbContext)
    {
        _mapper = mapper;
        _passengerDbContext = passengerDbContext;
    }

    public async Task<PassengerResponseDto> Handle(CompleteRegisterPassengerCommand command,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(command, nameof(command));

        Models.Passenger passenger = await _passengerDbContext.Passengers.AsNoTracking().SingleOrDefaultAsync(
                                      x => x.PassportNumber == command.PassportNumber, cancellationToken);

        if (passenger is null)
            throw new PassengerNotExist();

        Models.Passenger passengerEntity
            = passenger.CompleteRegistrationPassenger(passenger.Id, passenger.Name, passenger.PassportNumber,
                                                      command.PassengerType, command.Age);

        EntityEntry<Models.Passenger> updatePassenger = _passengerDbContext.Passengers.Update(passengerEntity);

        return _mapper.Map<PassengerResponseDto>(updatePassenger.Entity);
    }
}
