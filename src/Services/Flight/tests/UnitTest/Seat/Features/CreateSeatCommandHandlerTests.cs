﻿using System;
using System.Threading;
using System.Threading.Tasks;

using Flight.Seats.Dtos;
using Flight.Seats.Features.CreateSeat;

using FluentAssertions;

using Unit.Test.Common;
using Unit.Test.Fakes;

using Xunit;

namespace Unit.Test.Seat.Features;

[Collection(nameof(UnitTestFixture))]
public class CreateSeatCommandHandlerTests
{
    private readonly UnitTestFixture _fixture;
    private readonly CreateSeatCommandHandler _handler;


    public CreateSeatCommandHandlerTests(UnitTestFixture fixture)
    {
        _fixture = fixture;
        _handler = new CreateSeatCommandHandler(_fixture.Mapper, _fixture.DbContext);
    }

    public Task<SeatResponseDto> Act(CreateSeatCommand command, CancellationToken cancellationToken)
    {
        return _handler.Handle(command, cancellationToken);
    }

    [Fact]
    public async Task handler_with_valid_command_should_create_new_seat_and_return_currect_seat_dto()
    {
        // Arrange
        CreateSeatCommand command = new FakeCreateSeatCommand().Generate();

        // Act
        SeatResponseDto response = await Act(command, CancellationToken.None);

        // Assert
        global::Flight.Seats.Models.Seat entity = await _fixture.DbContext.Seats.FindAsync(response?.Id);

        entity?.Should().NotBeNull();
        response?.Id.Should().Be(entity?.Id);
    }

    [Fact]
    public async Task handler_with_null_command_should_throw_argument_exception()
    {
        // Arrange
        CreateSeatCommand command = null;

        // Act
        Func<Task> act = async () => { await Act(command, CancellationToken.None); };

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
