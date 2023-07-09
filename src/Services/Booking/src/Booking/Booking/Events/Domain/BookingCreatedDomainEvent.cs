using Booking.Booking.Models.ValueObjects;

using BuildingBlocks.Core.Event;

namespace Booking.Booking.Events.Domain;

public record BookingCreatedDomainEvent(long Id, PassengerInfo PassengerInfo, Trip Trip, bool IsDeleted) : IDomainEvent;
