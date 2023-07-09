using System.Threading.Tasks;

using BuildingBlocks.Contracts.Grpc;
using BuildingBlocks.TestBase;

using Flight.Api;
using Flight.Data;
using Flight.Flights.Features.CreateFlight;
using Flight.Flights.Features.CreateFlight.Reads;
using Flight.Seats.Features.CreateSeat;
using Flight.Seats.Features.CreateSeat.Reads;

using FluentAssertions;

using Grpc.Net.Client;

using Integration.Test.Fakes;

using MagicOnion.Client;

using Xunit;

namespace Integration.Test.Seat.Features;

public class ReserveSeatTests : IntegrationTestBase<Program, FlightDbContext, FlightReadDbContext>
{
    private readonly GrpcChannel _channel;

    public ReserveSeatTests(
        IntegrationTestFixture<Program, FlightDbContext, FlightReadDbContext> integrationTestFixture) : base(
     integrationTestFixture)
    {
        _channel = Fixture.Channel;
    }

    [Fact]
    public async Task should_return_valid_reserve_seat_from_grpc_service()
    {
        // Arrange
        CreateFlightCommand flightCommand = new FakeCreateFlightCommand().Generate();

        await Fixture.SendAsync(flightCommand);

        await Fixture.ShouldProcessedPersistInternalCommand<CreateFlightMongoCommand>();

        CreateSeatCommand seatCommand = new FakeCreateSeatCommand(flightCommand.Id).Generate();

        await Fixture.SendAsync(seatCommand);

        await Fixture.ShouldProcessedPersistInternalCommand<CreateSeatMongoCommand>();

        IFlightGrpcService flightGrpcClient = MagicOnionClient.Create<IFlightGrpcService>(_channel);

        // Act
        SeatResponseDto response = await flightGrpcClient.ReserveSeat(new ReserveSeatRequestDto
        {
            FlightId = seatCommand.FlightId,
            SeatNumber = seatCommand.SeatNumber
        });

        // Assert
        response?.Should().NotBeNull();
        response?.SeatNumber.Should().Be(seatCommand.SeatNumber);
        response?.FlightId.Should().Be(seatCommand.FlightId);
    }
}
