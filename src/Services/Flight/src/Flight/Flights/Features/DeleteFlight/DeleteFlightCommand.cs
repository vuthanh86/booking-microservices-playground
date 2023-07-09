﻿using BuildingBlocks.Core.CQRS;
using BuildingBlocks.Core.Event;

using Flight.Flights.Dtos;

namespace Flight.Flights.Features.DeleteFlight;

public record DeleteFlightCommand(long Id) : ICommand<FlightResponseDto>, IInternalCommand;
