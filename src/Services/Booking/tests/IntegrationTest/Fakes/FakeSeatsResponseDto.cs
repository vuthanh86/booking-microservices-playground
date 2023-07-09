using System.Collections.Generic;

using BuildingBlocks.Contracts.Grpc;

namespace Integration.Test.Fakes;

public static class FakeSeatsResponseDto
{
    public static IEnumerable<SeatResponseDto> Generate()
    {
        return new List<SeatResponseDto>
        {
            new()
            {
                FlightId = 1,
                Class = SeatClass.Economy,
                Type = SeatType.Aisle,
                SeatNumber = "33F",
                Id = 1
            },
            new()
            {
                FlightId = 1,
                Class = SeatClass.Economy,
                Type = SeatType.Window,
                SeatNumber = "22D",
                Id = 2
            }
        };
    }
}
