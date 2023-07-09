using BuildingBlocks.Exception;

namespace Flight.Seats.Exceptions;

public class SeatAlreadyExistException : ConflictException
{
    public SeatAlreadyExistException(int? code = default) : base("Seat already exist!", code)
    { }
}
