using System.Text.Json;

using BuildingBlocks.Core;
using BuildingBlocks.Core.Event;

using MediatR;

using Microsoft.Extensions.Logging;

namespace BuildingBlocks.EFCore;

public class EfTxBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    private readonly IDbContext _dbContextBase;
    private readonly IEventDispatcher _eventDispatcher;
    private readonly ILogger<EfTxBehavior<TRequest, TResponse>> _logger;

    public EfTxBehavior(
        ILogger<EfTxBehavior<TRequest, TResponse>> logger,
        IDbContext dbContextBase,
        IEventDispatcher eventDispatcher)
    {
        _logger = logger;
        _dbContextBase = dbContextBase;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        _logger.LogInformation(
                               "{Prefix} Handled command {MediatrRequest}",
                               nameof(EfTxBehavior<TRequest, TResponse>),
                               typeof(TRequest).FullName);

        _logger.LogDebug(
                         "{Prefix} Handled command {MediatrRequest} with content {RequestContent}",
                         nameof(EfTxBehavior<TRequest, TResponse>),
                         typeof(TRequest).FullName,
                         JsonSerializer.Serialize(request));

        _logger.LogInformation(
                               "{Prefix} Open the transaction for {MediatrRequest}",
                               nameof(EfTxBehavior<TRequest, TResponse>),
                               typeof(TRequest).FullName);

        await _dbContextBase.BeginTransactionAsync(cancellationToken);

        try
        {
            TResponse response = await next();

            _logger.LogInformation(
                                   "{Prefix} Executed the {MediatrRequest} request",
                                   nameof(EfTxBehavior<TRequest, TResponse>),
                                   typeof(TRequest).FullName);

            await _dbContextBase.CommitTransactionAsync(cancellationToken);

            IReadOnlyList<IDomainEvent> domainEvents = _dbContextBase.GetDomainEvents();

            EventType eventType = typeof(TRequest).IsAssignableTo(typeof(IInternalCommand))
                                      ? EventType.InternalCommand
                                      : EventType.DomainEvent;

            await _eventDispatcher.SendAsync(domainEvents.ToArray(), eventType, cancellationToken);

            return response;
        }
        catch (System.Exception ex)
        {
            await _dbContextBase.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
