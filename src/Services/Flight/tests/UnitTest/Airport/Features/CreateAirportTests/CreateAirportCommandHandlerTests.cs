using System;
using System.Threading;
using System.Threading.Tasks;

using Flight.Airports.Dtos;
using Flight.Airports.Features.CreateAirport;

using FluentAssertions;

using Unit.Test.Common;
using Unit.Test.Fakes;

using Xunit;

namespace Unit.Test.Airport.Features.CreateAirportTests;

[Collection(nameof(UnitTestFixture))]
public class CreateAirportCommandHandlerTests
{
    private readonly UnitTestFixture _fixture;
    private readonly CreateAirportCommandHandler _handler;


    public CreateAirportCommandHandlerTests(UnitTestFixture fixture)
    {
        _fixture = fixture;
        _handler = new CreateAirportCommandHandler(_fixture.Mapper, _fixture.DbContext);
    }

    public Task<AirportResponseDto> Act(CreateAirportCommand command, CancellationToken cancellationToken)
    {
        return _handler.Handle(command, cancellationToken);
    }

    [Fact]
    public async Task handler_with_valid_command_should_create_new_airport_and_return_currect_airport_dto()
    {
        // Arrange
        CreateAirportCommand command = new FakeCreateAirportCommand().Generate();

        // Act
        AirportResponseDto response = await Act(command, CancellationToken.None);

        // Assert
        global::Flight.Airports.Models.Airport entity = await _fixture.DbContext.Airports.FindAsync(response?.Id);

        entity?.Should().NotBeNull();
        response?.Id.Should().Be(entity?.Id);
    }

    [Fact]
    public async Task handler_with_null_command_should_throw_argument_exception()
    {
        // Arrange
        CreateAirportCommand command = null;

        // Act
        Func<Task> act = async () => { await Act(command, CancellationToken.None); };

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
