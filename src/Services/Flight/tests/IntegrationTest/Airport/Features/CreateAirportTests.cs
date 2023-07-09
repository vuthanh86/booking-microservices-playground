using System.Threading.Tasks;

using BuildingBlocks.Contracts.EventBus.Messages;
using BuildingBlocks.TestBase;

using Flight.Airports.Dtos;
using Flight.Airports.Features.CreateAirport;
using Flight.Airports.Features.CreateAirport.Reads;
using Flight.Api;
using Flight.Data;

using FluentAssertions;

using Integration.Test.Fakes;

using MassTransit;
using MassTransit.Testing;

using Xunit;

namespace Integration.Test.Airport.Features;

public class CreateAirportTests : IntegrationTestBase<Program, FlightDbContext, FlightReadDbContext>
{
    private readonly ITestHarness _testHarness;

    public CreateAirportTests(
        IntegrationTestFixture<Program, FlightDbContext, FlightReadDbContext> integrationTestFixture) : base(
     integrationTestFixture)
    {
        _testHarness = Fixture.TestHarness;
    }

    [Fact]
    public async Task should_create_new_airport_to_db_and_publish_message_to_broker()
    {
        // Arrange
        CreateAirportCommand command = new FakeCreateAirportCommand().Generate();

        // Act
        AirportResponseDto response = await Fixture.SendAsync(command);

        // Assert
        response?.Should().NotBeNull();
        response?.Name.Should().Be(command.Name);
        (await _testHarness.Published.Any<Fault<AirportCreated>>()).Should().BeFalse();
        (await _testHarness.Published.Any<AirportCreated>()).Should().BeTrue();

        await Fixture.ShouldProcessedPersistInternalCommand<CreateAirportMongoCommand>();
    }
}
