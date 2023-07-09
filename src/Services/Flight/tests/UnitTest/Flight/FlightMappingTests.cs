﻿using System;
using System.Collections.Generic;

using Flight.Flights.Dtos;

using MapsterMapper;

using Unit.Test.Common;

using Xunit;

namespace Unit.Test.Flight;

[Collection(nameof(UnitTestFixture))]
public class FlightMappingTests
{
    private readonly UnitTestFixture _fixture;
    private readonly IMapper _mapper;

    public FlightMappingTests(UnitTestFixture fixture)
    {
        _mapper = fixture.Mapper;
    }

    public static IEnumerable<object[]> Data
    {
        get
        {
            yield return new object[]
            {
                // these types will instantiate with reflection in the future
                typeof(global::Flight.Flights.Models.Flight), typeof(FlightResponseDto)
            };
        }
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void should_support_mapping_from_source_to_destination(Type source, Type destination,
        params object[] parameters)
    {
        object instance = Activator.CreateInstance(source, parameters);

        _mapper.Map(instance, source, destination);
    }
}
