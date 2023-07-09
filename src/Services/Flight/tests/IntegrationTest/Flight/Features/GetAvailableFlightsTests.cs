using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BuildingBlocks.TestBase;

using Flight.Api;
using Flight.Data;
using Flight.Flights.Dtos;
using Flight.Flights.Features.CreateFlight;
using Flight.Flights.Features.CreateFlight.Reads;
using Flight.Flights.Features.GetAvailableFlights;

using FluentAssertions;

using Integration.Test.Fakes;

using Xunit;

namespace Integration.Test.Flight.Features;

public class GetAvailableFlightsTests : IntegrationTestBase<Program, FlightDbContext, FlightReadDbContext>
{
    public GetAvailableFlightsTests(
        IntegrationTestFixture<Program, FlightDbContext, FlightReadDbContext> integrationTestFixture)
        : base(integrationTestFixture)
    { }

    [Fact]
    public async Task should_return_available_flights()
    {
        // Arrange
        CreateFlightCommand flightCommand = new FakeCreateFlightCommand().Generate();

        await Fixture.SendAsync(flightCommand);

        await Fixture.ShouldProcessedPersistInternalCommand<CreateFlightMongoCommand>();

        GetAvailableFlightsQuery query = new GetAvailableFlightsQuery();

        // Act
        List<FlightResponseDto> response = (await Fixture.SendAsync(query))?.ToList();

        // Assert
        response?.Should().NotBeNull();
        response?.Count().Should().BeGreaterOrEqualTo(2);
    }
}
