using System.Reflection;

using BuildingBlocks.EventStoreDB.BackgroundWorkers;
using BuildingBlocks.EventStoreDB.Events;
using BuildingBlocks.EventStoreDB.Projections;
using BuildingBlocks.EventStoreDB.Repository;
using BuildingBlocks.EventStoreDB.Subscriptions;

using EventStore.Client;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.EventStoreDB;

public class EventStoreDbConfig
{
    public string ConnectionString { get; set; } = default!;
}

public record EventStoreDbOptions(
    bool UseInternalCheckpointing = true
);

public static class EventStoreDbConfigExtensions
{
    private const string DefaultConfigKey = "EventStore";

    public static IServiceCollection AddEventStoreDb(this IServiceCollection services, IConfiguration config,
        [CanBeNull] EventStoreDbOptions options = null)
    {
        EventStoreDbConfig eventStoreDbConfig = config.GetSection(DefaultConfigKey).Get<EventStoreDbConfig>();

        services
            .AddSingleton(new EventStoreClient(EventStoreClientSettings.Create(eventStoreDbConfig.ConnectionString)))
            .AddScoped(typeof(IEventStoreDbRepository<>), typeof(EventStoreDbRepository<>))
            .AddTransient<EventStoreDbSubscriptionToAll, EventStoreDbSubscriptionToAll>();

        if (options?.UseInternalCheckpointing != false)
            services.AddTransient<ISubscriptionCheckpointRepository, EventStoreDbSubscriptionCheckpointRepository>();

        return services;
    }

    public static IServiceCollection AddEventStoreDbSubscriptionToAll(
        this IServiceCollection services,
        [CanBeNull] EventStoreDbSubscriptionToAllOptions subscriptionOptions = null,
        bool checkpointToEventStoreDb = true)
    {
        if (checkpointToEventStoreDb)
            services.AddTransient<ISubscriptionCheckpointRepository, EventStoreDbSubscriptionCheckpointRepository>();

        return services.AddHostedService(serviceProvider =>
                                         {
                                             ILogger<BackgroundWorker> logger =
                                                 serviceProvider.GetRequiredService<ILogger<BackgroundWorker>>();

                                             EventStoreDbSubscriptionToAll eventStoreDbSubscriptionToAll =
                                                 serviceProvider.GetRequiredService<EventStoreDbSubscriptionToAll>();

                                             return new BackgroundWorker(
                                                                         logger,
                                                                         ct =>
                                                                             eventStoreDbSubscriptionToAll
                                                                                 .SubscribeToAll(
                                                                                      subscriptionOptions ??
                                                                                      new
                                                                                          EventStoreDbSubscriptionToAllOptions(),
                                                                                      ct
                                                                                     )
                                                                        );
                                         }
                                        );
    }

    public static IServiceCollection AddProjections(this IServiceCollection services,
        params Assembly[] assembliesToScan)
    {
        services.AddSingleton<IProjectionPublisher, ProjectionPublisher>();

        RegisterProjections(services, assembliesToScan!);

        return services;
    }

    private static void RegisterProjections(IServiceCollection services, Assembly[] assembliesToScan)
    {
        services.Scan(scan => scan
                          .FromAssemblies(assembliesToScan)
                          .AddClasses(classes => classes.AssignableTo<IProjection>()) // Filter classes
                          .AsImplementedInterfaces()
                          .WithTransientLifetime());
    }
}
