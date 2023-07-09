using System.Reflection;

using BuildingBlocks.EventStoreDB.Events;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.EventStoreDB.Projections;

public class ProjectionPublisher : IProjectionPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public ProjectionPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<T>(StreamEvent<T> streamEvent, CancellationToken cancellationToken = default)
        where T : INotification
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        IEnumerable<IProjectionProcessor> projectionsProcessors
            = scope.ServiceProvider.GetRequiredService<IEnumerable<IProjectionProcessor>>();
        foreach (IProjectionProcessor projectionProcessor in projectionsProcessors)
            await projectionProcessor.ProcessEventAsync(streamEvent, cancellationToken);
    }

    public Task PublishAsync(StreamEvent streamEvent, CancellationToken cancellationToken = default)
    {
        Type streamData = streamEvent.Data.GetType();

        MethodInfo method = typeof(IProjectionPublisher)
            .GetMethods()
            .Single(m => m.Name == nameof(PublishAsync) && m.GetGenericArguments().Any())
            .MakeGenericMethod(streamData);

        return (Task)method
            .Invoke(this, new object[] { streamEvent, cancellationToken })!;
    }
}
