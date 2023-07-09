using Ardalis.GuardClauses;

using Booking.Booking.Exceptions;
using Booking.Booking.Models.ValueObjects;

using BuildingBlocks.Contracts.Grpc;
using BuildingBlocks.Core.CQRS;
using BuildingBlocks.EventStoreDB.Repository;

namespace Booking.Booking.Features.CreateBooking;

public class CreateBookingCommandHandler : ICommandHandler<CreateBookingCommand, ulong>
{
    private readonly IEventStoreDbRepository<Models.Booking> _eventStoreDbRepository;
    private readonly IFlightGrpcService _flightGrpcService;
    private readonly IPassengerGrpcService _passengerGrpcService;

    public CreateBookingCommandHandler(IEventStoreDbRepository<Models.Booking> eventStoreDbRepository,
        IPassengerGrpcService passengerGrpcService,
        IFlightGrpcService flightGrpcService)
    {
        _eventStoreDbRepository = eventStoreDbRepository;
        _passengerGrpcService = passengerGrpcService;
        _flightGrpcService = flightGrpcService;
    }

    public async Task<ulong> Handle(CreateBookingCommand command,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(command, nameof(command));

        FlightResponseDto flight = await _flightGrpcService.GetById(command.FlightId);

        if (flight is null)
            throw new FlightNotFoundException();

        PassengerResponseDto passenger = await _passengerGrpcService.GetById(command.PassengerId);

        SeatResponseDto emptySeat = (await _flightGrpcService.GetAvailableSeats(command.FlightId))?.First();

        Models.Booking reservation = await _eventStoreDbRepository.FindAsync(command.Id, cancellationToken);

        if (reservation is not null && !reservation.IsDeleted)
            throw new BookingAlreadyExistException();

        Models.Booking aggrigate = Models.Booking.Create(command.Id, new PassengerInfo(passenger.Name), new Trip(
                                                          flight.FlightNumber, flight.AircraftId,
                                                          flight.DepartureAirportId,
                                                          flight.ArriveAirportId, flight.FlightDate, flight.Price,
                                                          command.Description, emptySeat?.SeatNumber));

        await _flightGrpcService.ReserveSeat(new ReserveSeatRequestDto
        {
            FlightId = flight.Id, SeatNumber = emptySeat?.SeatNumber
        });

        ulong result = await _eventStoreDbRepository.AddAsync(
                                                         aggrigate,
                                                         cancellationToken);

        return result;
    }
}
