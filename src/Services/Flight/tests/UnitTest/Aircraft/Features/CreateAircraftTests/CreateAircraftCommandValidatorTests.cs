﻿using Flight.Aircrafts.Features.CreateAircraft;

using FluentAssertions;

using FluentValidation.TestHelper;

using Unit.Test.Common;
using Unit.Test.Fakes;

using Xunit;

namespace Unit.Test.Aircraft.Features.CreateAircraftTests;

[Collection(nameof(UnitTestFixture))]
public class CreateAircraftCommandValidatorTests
{
    [Fact]
    public void is_valid_should_be_false_when_have_invalid_parameter()
    {
        // Arrange
        CreateAircraftCommand command = new FakeValidateCreateAircraftCommand().Generate();
        CreateAircraftCommandValidator validator = new CreateAircraftCommandValidator();

        // Act
        TestValidationResult<CreateAircraftCommand> result = validator.TestValidate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Model);
        result.ShouldHaveValidationErrorFor(x => x.ManufacturingYear);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
