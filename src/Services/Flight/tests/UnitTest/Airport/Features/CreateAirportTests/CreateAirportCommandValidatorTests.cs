﻿using Flight.Airports.Features.CreateAirport;

using FluentAssertions;

using FluentValidation.TestHelper;

using Unit.Test.Common;
using Unit.Test.Fakes;

using Xunit;

namespace Unit.Test.Airport.Features.CreateAirportTests;

[Collection(nameof(UnitTestFixture))]
public class CreateAirportCommandValidatorTests
{
    [Fact]
    public void is_valid_should_be_false_when_have_invalid_parameter()
    {
        // Arrange
        CreateAirportCommand command = new FakeValidateCreateAirportCommand().Generate();
        CreateAirportCommandValidator validator = new CreateAirportCommandValidator();

        // Act
        TestValidationResult<CreateAirportCommand> result = validator.TestValidate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Code);
        result.ShouldHaveValidationErrorFor(x => x.Address);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
