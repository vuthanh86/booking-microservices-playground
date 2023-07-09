using BuildingBlocks.EventStoreDB.Events;
using BuildingBlocks.EventStoreDB.Projections;
using BuildingBlocks.Utils;

using EventStore.Client;

using Grpc.Core;

using JetBrains.Annotations;

using MediatR;

using Microsoft.Extensions.Logging;

namespace BuildingBlocks.EventStoreDB.Subscriptions;

public class EventStoreDbSubscriptionToAllOptions
{
    public string SubscriptionId { get; set; } = "default";

    public SubscriptionFilterOptions FilterOptions { get; set; } =
        new(EventTypeFilter.ExcludeSystemEvents());

    [CanBeNull] public Action<EventStoreClientOperationOptions> ConfigureOperation { get; set; }
    [CanBeNull] public UserCredentials Credentials { get; set; }
    public bool ResolveLinkTos { get; set; }
    public bool IgnoreDeserializationErrors { get; set; } = true;
}

public class EventStoreDbSubscriptionToAll
{
    private readonly IMediator _mediator;
    private readonly ISubscriptionCheckpointRepository _checkpointRepository;
    private readonly EventStoreClient _eventStoreClient;
    private readonly ILogger<EventStoreDbSubscriptionToAll> _logger;
    private readonly IProjectionPublisher _projectionPublisher;
    private readonly object _resubscribeLock = new();
    private CancellationToken _cancellationToken;
    private EventStoreDbSubscriptionToAllOptions _subscriptionOptions = default!;

    public EventStoreDbSubscriptionToAll(
        EventStoreClient eventStoreClient,
        IMediator mediator,
        IProjectionPublisher projectionPublisher,
        ISubscriptionCheckpointRepository checkpointRepository,
        ILogger<EventStoreDbSubscriptionToAll> logger
    )
    {
        this._projectionPublisher = projectionPublisher;
        this._eventStoreClient = eventStoreClient ?? throw new ArgumentNullException(nameof(eventStoreClient));
        _mediator = mediator;
        this._checkpointRepository =
            checkpointRepository ?? throw new ArgumentNullException(nameof(checkpointRepository));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string SubscriptionId => _subscriptionOptions.SubscriptionId;

    public async Task SubscribeToAll(EventStoreDbSubscriptionToAllOptions subscriptionOptions, CancellationToken ct)
    {
        // see: https://github.com/dotnet/runtime/issues/36063
        await Task.Yield();

        this._subscriptionOptions = subscriptionOptions;
        _cancellationToken = ct;

        _logger.LogInformation("Subscription to all '{SubscriptionId}'", subscriptionOptions.SubscriptionId);

        ulong? checkpoint = await _checkpointRepository.Load(SubscriptionId, ct);

        await _eventStoreClient.SubscribeToAllAsync(
                                                   checkpoint == null
                                                       ? FromAll.Start
                                                       : FromAll.After(new Position(checkpoint.Value,
                                                                        checkpoint.Value)),
                                                   HandleEvent,
                                                   subscriptionOptions.ResolveLinkTos,
                                                   HandleDrop,
                                                   subscriptionOptions.FilterOptions,
                                                   subscriptionOptions.Credentials,
                                                   ct
                                                  );

        _logger.LogInformation("Subscription to all '{SubscriptionId}' started", SubscriptionId);
    }

    private async Task HandleEvent(StreamSubscription subscription, ResolvedEvent resolvedEvent,
        CancellationToken ct)
    {
        try
        {
            if (IsEventWithEmptyData(resolvedEvent) || IsCheckpointEvent(resolvedEvent)) return;

            StreamEvent streamEvent = resolvedEvent.ToStreamEvent();

            if (streamEvent == null)
            {
                // That can happen if we're sharing database between modules.
                // If we're subscribing to all and not filtering out events from other modules,
                // then we might get events that are from other module and we might not be able to deserialize them.
                // In that case it's safe to ignore deserialization error.
                // You may add more sophisticated logic checking if it should be ignored or not.
                _logger.LogWarning("Couldn't deserialize event with id: {EventId}", resolvedEvent.Event.EventId);

                if (!_subscriptionOptions.IgnoreDeserializationErrors)
                    throw new
                        InvalidOperationException($"Unable to deserialize event {resolvedEvent.Event.EventType} with id: {resolvedEvent.Event.EventId}");
                return;
            }

            // publish event to internal event bus
            await _mediator.Publish(streamEvent, ct);

            await _projectionPublisher.PublishAsync(streamEvent, ct);

            await _checkpointRepository.Store(SubscriptionId, resolvedEvent.Event.Position.CommitPosition, ct);
        }
        catch (System.Exception e)
        {
            _logger.LogError("Error consuming message: {ExceptionMessage}{ExceptionStackTrace}", e.Message,
                            e.StackTrace);
            // if you're fine with dropping some events instead of stopping subscription
            // then you can add some logic if error should be ignored
            throw;
        }
    }

    private void HandleDrop(StreamSubscription _, SubscriptionDroppedReason reason, [CanBeNull] System.Exception exception)
    {
        _logger.LogError(
                        exception,
                        "Subscription to all '{SubscriptionId}' dropped with '{Reason}'",
                        SubscriptionId,
                        reason
                       );

        if (exception is RpcException { StatusCode: StatusCode.Cancelled })
            return;

        Resubscribe();
    }

    private void Resubscribe()
    {
        // You may consider adding a max resubscribe count if you want to fail process
        // instead of retrying until database is up
        while (true)
        {
            bool resubscribed = false;
            try
            {
                Monitor.Enter(_resubscribeLock);

                // No synchronization context is needed to disable synchronization context.
                // That enables running asynchronous method not causing deadlocks.
                // As this is a background process then we don't need to have async context here.
                using (NoSynchronizationContextScope.Enter())
                {
                    SubscribeToAll(_subscriptionOptions, _cancellationToken).Wait(_cancellationToken);
                }

                resubscribed = true;
            }
            catch (System.Exception exception)
            {
                _logger.LogWarning(exception,
                                  "Failed to resubscribe to all '{SubscriptionId}' dropped with '{ExceptionMessage}{ExceptionStackTrace}'",
                                  SubscriptionId, exception.Message, exception.StackTrace);
            }
            finally
            {
                Monitor.Exit(_resubscribeLock);
            }

            if (resubscribed)
                break;

            // Sleep between reconnections to not flood the database or not kill the CPU with infinite loop
            // Randomness added to reduce the chance of multiple subscriptions trying to reconnect at the same time
            Thread.Sleep(1000 + new Random((int)DateTime.UtcNow.Ticks).Next(1000));
        }
    }

    private bool IsEventWithEmptyData(ResolvedEvent resolvedEvent)
    {
        if (resolvedEvent.Event.Data.Length != 0) return false;

        _logger.LogInformation("Event without data received");
        return true;
    }

    private bool IsCheckpointEvent(ResolvedEvent resolvedEvent)
    {
        if (resolvedEvent.Event.EventType != EventTypeMapper.ToName<CheckpointStored>()) return false;

        _logger.LogInformation("Checkpoint event - ignoring");
        return true;
    }
}
