﻿using System.Threading.Tasks;

using BuildingBlocks.Contracts.EventBus.Messages;
using BuildingBlocks.TestBase;

using Flight.Aircrafts.Dtos;
using Flight.Aircrafts.Features.CreateAircraft;
using Flight.Aircrafts.Features.CreateAircraft.Reads;
using Flight.Api;
using Flight.Data;

using FluentAssertions;

using Integration.Test.Fakes;

using MassTransit;
using MassTransit.Testing;

using Xunit;

namespace Integration.Test.Aircraft.Features;

public class CreateAircraftTests : IntegrationTestBase<Program, FlightDbContext, FlightReadDbContext>
{
    private readonly ITestHarness _testHarness;

    public CreateAircraftTests(
        IntegrationTestFixture<Program, FlightDbContext, FlightReadDbContext> integrationTestFixture) :
        base(integrationTestFixture)
    {
        _testHarness = Fixture.TestHarness;
    }

    [Fact]
    public async Task should_create_new_aircraft_to_db_and_publish_message_to_broker()
    {
        // Arrange
        CreateAircraftCommand command = new FakeCreateAircraftCommand().Generate();

        // Act
        AircraftResponseDto response = await Fixture.SendAsync(command);

        // Assert
        response?.Should().NotBeNull();
        response?.Name.Should().Be(command.Name);
        (await _testHarness.Published.Any<Fault<AircraftCreated>>()).Should().BeFalse();
        (await _testHarness.Published.Any<AircraftCreated>()).Should().BeTrue();

        await Fixture.ShouldProcessedPersistInternalCommand<CreateAircraftMongoCommand>();
    }
}
