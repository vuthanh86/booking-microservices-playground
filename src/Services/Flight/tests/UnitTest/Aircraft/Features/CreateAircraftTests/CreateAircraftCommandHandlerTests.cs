﻿using System;
using System.Threading;
using System.Threading.Tasks;

using Flight.Aircrafts.Dtos;
using Flight.Aircrafts.Features.CreateAircraft;

using FluentAssertions;

using Unit.Test.Common;
using Unit.Test.Fakes;

using Xunit;

namespace Unit.Test.Aircraft.Features.CreateAircraftTests;

[Collection(nameof(UnitTestFixture))]
public class CreateAircraftCommandHandlerTests
{
    private readonly UnitTestFixture _fixture;
    private readonly CreateAircraftCommandHandler _handler;

    public CreateAircraftCommandHandlerTests(UnitTestFixture fixture)
    {
        _fixture = fixture;
        _handler = new CreateAircraftCommandHandler(_fixture.Mapper, _fixture.DbContext);
    }

    public Task<AircraftResponseDto> Act(CreateAircraftCommand command, CancellationToken cancellationToken)
    {
        return _handler.Handle(command, cancellationToken);
    }

    [Fact]
    public async Task handler_with_valid_command_should_create_new_aircraft_and_return_currect_aircraft_dto()
    {
        // Arrange
        CreateAircraftCommand command = new FakeCreateAircraftCommand().Generate();

        // Act
        AircraftResponseDto response = await Act(command, CancellationToken.None);

        // Assert
        global::Flight.Aircrafts.Models.Aircraft entity = await _fixture.DbContext.Aircraft.FindAsync(response?.Id);

        entity?.Should().NotBeNull();
        response?.Id.Should().Be(entity?.Id);
    }

    [Fact]
    public async Task handler_with_null_command_should_throw_argument_exception()
    {
        // Arrange
        CreateAircraftCommand command = null;

        // Act
        Func<Task> act = async () => { await Act(command, CancellationToken.None); };

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
