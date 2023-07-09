﻿using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text.Json;

using Ardalis.GuardClauses;

using BuildingBlocks.Core.Event;
using BuildingBlocks.EFCore;
using BuildingBlocks.Utils;

using MassTransit;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.MessageProcessor;

public class PersistMessageProcessor : IPersistMessageProcessor
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<PersistMessageProcessor> _logger;
    private readonly IMediator _mediator;
    private readonly IPublishEndpoint _publishEndpoint;

    public PersistMessageProcessor(
        ILogger<PersistMessageProcessor> logger,
        IMediator mediator,
        IDbContext dbContext,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _mediator = mediator;
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishMessageAsync<TMessageEnvelope>(
        TMessageEnvelope messageEnvelope,
        CancellationToken cancellationToken = default)
        where TMessageEnvelope : MessageEnvelope
    {
        await SavePersistMessageAsync(messageEnvelope, MessageDeliveryType.Outbox, cancellationToken);
    }

    public Task<Guid> AddReceivedMessageAsync<TMessageEnvelope>(TMessageEnvelope messageEnvelope,
        CancellationToken cancellationToken = default) where TMessageEnvelope : MessageEnvelope
    {
        return SavePersistMessageAsync(messageEnvelope, MessageDeliveryType.Inbox, cancellationToken);
    }

    public async Task AddInternalMessageAsync<TCommand>(TCommand internalCommand,
        CancellationToken cancellationToken = default) where TCommand : class, IInternalCommand
    {
        await SavePersistMessageAsync(new MessageEnvelope(internalCommand), MessageDeliveryType.Internal,
                                      cancellationToken);
    }

    public async Task<IReadOnlyList<PersistMessage>> GetByFilterAsync(Expression<Func<PersistMessage, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ReadOnlyCollection<PersistMessage> b
            = (await _dbContext.PersistMessages.Where(predicate).ToListAsync(cancellationToken)).AsReadOnly();
        return b;
    }

    public Task<PersistMessage> ExistMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PersistMessages.FirstOrDefaultAsync(x =>
                                                                  x.Id == messageId &&
                                                                  x.DeliveryType == MessageDeliveryType.Inbox &&
                                                                  x.MessageStatus == MessageStatus.Processed,
                                                              cancellationToken);
    }

    public async Task ProcessAsync(
        Guid messageId,
        MessageDeliveryType deliveryType,
        CancellationToken cancellationToken = default)
    {
        PersistMessage message =
            await _dbContext.PersistMessages.FirstOrDefaultAsync(
                                                                 x => x.Id == messageId &&
                                                                      x.DeliveryType == deliveryType,
                                                                 cancellationToken);

        if (message is null)
            return;

        switch (deliveryType)
        {
            case MessageDeliveryType.Internal:
                await ProcessInternalAsync(message, cancellationToken);
                break;
            case MessageDeliveryType.Outbox:
                await ProcessOutboxAsync(message, cancellationToken);
                break;
        }

        await ChangeMessageStatusAsync(message, cancellationToken);
    }


    public async Task ProcessAllAsync(CancellationToken cancellationToken = default)
    {
        List<PersistMessage> messages = await _dbContext.PersistMessages
                                            .Where(x => x.MessageStatus != MessageStatus.Processed)
                                            .ToListAsync(cancellationToken);

        foreach (PersistMessage message in messages)
            await ProcessAsync(message.Id, message.DeliveryType, cancellationToken);
    }

    public async Task ProcessInboxAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        PersistMessage message = await _dbContext.PersistMessages.FirstOrDefaultAsync(
                                  x => x.Id == messageId &&
                                       x.DeliveryType == MessageDeliveryType.Inbox &&
                                       x.MessageStatus == MessageStatus.InProgress,
                                  cancellationToken);

        await ChangeMessageStatusAsync(message, cancellationToken);
    }

    private async Task<Guid> SavePersistMessageAsync(
        MessageEnvelope messageEnvelope,
        MessageDeliveryType deliveryType,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(messageEnvelope.Message, nameof(messageEnvelope.Message));

        Guid id;
        if (messageEnvelope.Message is IEvent message)
            id = message.EventId;
        else
            id = Guid.NewGuid();

        await _dbContext.PersistMessages.AddAsync(
                                                  new PersistMessage(
                                                                     id,
                                                                     TypeProvider.GetTypeName(messageEnvelope.Message
                                                                         .GetType()),
                                                                     JsonSerializer.Serialize(messageEnvelope),
                                                                     deliveryType),
                                                  cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
                               "Message with id: {MessageID} and delivery type: {DeliveryType} saved in persistence message store.",
                               id,
                               deliveryType.ToString());

        return id;
    }

    private async Task ProcessOutboxAsync(PersistMessage message, CancellationToken cancellationToken)
    {
        MessageEnvelope? messageEnvelope = JsonSerializer.Deserialize<MessageEnvelope>(message.Data);

        if (messageEnvelope is null || messageEnvelope.Message is null)
            return;

        object data = JsonSerializer.Deserialize(messageEnvelope.Message.ToString()!,
                                                 TypeProvider.GetType(message.DataType));

        if (data is IEvent)
        {
            await _publishEndpoint.Publish(data, context =>
            {
                foreach (KeyValuePair<string, object> header in messageEnvelope.Headers)
                    context.Headers.Set(header.Key, header.Value);
            }, cancellationToken);

            _logger.LogInformation(
                                   "Message with id: {MessageId} and delivery type: {DeliveryType} processed from the persistence message store.",
                                   message.Id,
                                   message.DeliveryType);
        }
    }

    private async Task ProcessInternalAsync(PersistMessage message, CancellationToken cancellationToken)
    {
        MessageEnvelope? messageEnvelope = JsonSerializer.Deserialize<MessageEnvelope>(message.Data);

        if (messageEnvelope is null || messageEnvelope.Message is null)
            return;

        object data = JsonSerializer.Deserialize(messageEnvelope.Message.ToString()!,
                                                 TypeProvider.GetType(message.DataType));

        if (data is IInternalCommand internalCommand)
        {
            await _mediator.Send(internalCommand, cancellationToken);

            _logger.LogInformation(
                                   "InternalCommand with id: {EventID} and delivery type: {DeliveryType} processed from the persistence message store.",
                                   message.Id,
                                   message.DeliveryType);
        }
    }

    private async Task ChangeMessageStatusAsync(PersistMessage message, CancellationToken cancellationToken)
    {
        message.ChangeState(MessageStatus.Processed);

        _dbContext.PersistMessages.Update(message);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
