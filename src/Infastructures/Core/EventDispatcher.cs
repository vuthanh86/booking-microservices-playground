using System.Security.Claims;

using BuildingBlocks.Core.Event;
using BuildingBlocks.MessageProcessor;
using BuildingBlocks.Web;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Core;

public sealed class EventDispatcher : IEventDispatcher
{
    private readonly IEventMapper _eventMapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EventDispatcher> _logger;
    private readonly IPersistMessageProcessor _persistMessageProcessor;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public EventDispatcher(IServiceScopeFactory serviceScopeFactory,
        IEventMapper eventMapper,
        ILogger<EventDispatcher> logger,
        IPersistMessageProcessor persistMessageProcessor,
        IHttpContextAccessor httpContextAccessor)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _eventMapper = eventMapper;
        _logger = logger;
        _persistMessageProcessor = persistMessageProcessor;
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task SendAsync<T>(IReadOnlyList<T> events, EventType eventType = default,
        CancellationToken cancellationToken = default)
        where T : IEvent
    {
        if (events.Count > 0)
        {
            async Task PublishIntegrationEvent(IReadOnlyList<IIntegrationEvent> integrationEvents)
            {
                foreach (IIntegrationEvent integrationEvent in integrationEvents)
                    await _persistMessageProcessor.PublishMessageAsync(
                                                                       new MessageEnvelope(integrationEvent,
                                                                        SetHeaders()),
                                                                       cancellationToken);
            }

            switch (events)
            {
                case IReadOnlyList<IDomainEvent> domainEvents:
                {
                    IReadOnlyList<IIntegrationEvent> integrationEvents
                        = await MapDomainEventToIntegrationEventAsync(domainEvents)
                              .ConfigureAwait(false);

                    await PublishIntegrationEvent(integrationEvents);
                    break;
                }

                case IReadOnlyList<IIntegrationEvent> integrationEvents:
                    await PublishIntegrationEvent(integrationEvents);
                    break;
            }

            if (eventType == EventType.InternalCommand)
            {
                IReadOnlyList<InternalCommand> internalMessages
                    = await MapDomainEventToInternalCommandAsync(events as IReadOnlyList<IDomainEvent>)
                          .ConfigureAwait(false);

                foreach (InternalCommand internalMessage in internalMessages)
                    await _persistMessageProcessor.AddInternalMessageAsync(internalMessage, cancellationToken);
            }
        }
    }

    public async Task SendAsync<T>(T @event, EventType eventType = default,
        CancellationToken cancellationToken = default)
        where T : IEvent
    {
        await SendAsync(new[] { @event }, eventType, cancellationToken);
    }


    private Task<IReadOnlyList<IIntegrationEvent>> MapDomainEventToIntegrationEventAsync(
        IReadOnlyList<IDomainEvent> events)
    {
        _logger.LogTrace("Processing integration events start...");

        List<IIntegrationEvent> wrappedIntegrationEvents = GetWrappedIntegrationEvents(events.ToList())?.ToList();
        if (wrappedIntegrationEvents?.Count > 0)
            return Task.FromResult<IReadOnlyList<IIntegrationEvent>>(wrappedIntegrationEvents);

        List<IIntegrationEvent> integrationEvents = new List<IIntegrationEvent>();
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        foreach (IDomainEvent @event in events)
        {
            Type eventType = @event.GetType();
            _logger.LogTrace($"Handling domain event: {eventType.Name}");

            IIntegrationEvent integrationEvent = _eventMapper.MapToIntegrationEvent(@event);

            if (integrationEvent is null) continue;

            integrationEvents.Add(integrationEvent);
        }

        _logger.LogTrace("Processing integration events done...");

        return Task.FromResult<IReadOnlyList<IIntegrationEvent>>(integrationEvents);
    }


    private Task<IReadOnlyList<InternalCommand>> MapDomainEventToInternalCommandAsync(
        IReadOnlyList<IDomainEvent> events)
    {
        _logger.LogTrace("Processing internal message start...");

        List<InternalCommand> internalCommands = new List<InternalCommand>();
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        foreach (IDomainEvent @event in events)
        {
            Type eventType = @event.GetType();
            _logger.LogTrace($"Handling domain event: {eventType.Name}");

            InternalCommand integrationEvent = _eventMapper.MapToInternalCommand(@event);

            if (integrationEvent is null) continue;

            internalCommands.Add(integrationEvent);
        }

        _logger.LogTrace("Processing internal message done...");

        return Task.FromResult<IReadOnlyList<InternalCommand>>(internalCommands);
    }

    private IEnumerable<IIntegrationEvent> GetWrappedIntegrationEvents(IReadOnlyList<IDomainEvent> domainEvents)
    {
        foreach (IDomainEvent domainEvent in domainEvents.Where(x =>
                                                                    x is IHaveIntegrationEvent))
        {
            Type genericType = typeof(IntegrationEventWrapper<>)
                .MakeGenericType(domainEvent.GetType());

            IIntegrationEvent domainNotificationEvent = (IIntegrationEvent)Activator
                .CreateInstance(genericType, domainEvent);

            yield return domainNotificationEvent;
        }
    }

    private IDictionary<string, object> SetHeaders()
    {
        Dictionary<string, object> headers = new Dictionary<string, object>();
        headers.Add("CorrelationId", _httpContextAccessor?.HttpContext?.GetCorrelationId());
        headers.Add("UserId", _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier));
        headers.Add("UserName", _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.Name));

        return headers;
    }
}
