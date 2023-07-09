using System.Reflection;

using BuildingBlocks.Core.Event;
using BuildingBlocks.Web;

using Humanizer;

using MassTransit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.MassTransit;

public static class Extensions
{
    private static bool? _isRunningInContainer;

    private static bool IsRunningInContainer =>
        _isRunningInContainer ??=
            bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out bool inContainer) &&
            inContainer;

    public static IServiceCollection AddCustomMassTransit(this IServiceCollection services, Assembly assembly,
        IWebHostEnvironment env)
    {
        if (!env.IsEnvironment("test"))
            services.AddMassTransit(configure =>
            {
                configure.AddConsumers(assembly);

                configure.UsingRabbitMq((context, configurator) =>
                {
                    RabbitMqOptions rabbitMqOptions = services.GetOptions<RabbitMqOptions>("RabbitMq");
                    string host = IsRunningInContainer ? "rabbitmq" : rabbitMqOptions.HostName;

                    configurator.Host(host, h =>
                    {
                        h.Username(rabbitMqOptions.UserName);
                        h.Password(rabbitMqOptions.Password);
                    });

                    IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                        .Where(x => x.IsAssignableTo(typeof(IIntegrationEvent))
                                    && !x.IsInterface
                                    && !x.IsAbstract
                                    && !x.IsGenericType);

                    foreach (Type type in types)
                    {
                        List<Type> consumers = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                            .Where(x => x.IsAssignableTo(typeof(IConsumer<>).MakeGenericType(type))).ToList();

                        if (consumers.Any())
                            configurator.ReceiveEndpoint(
                                                         string.IsNullOrEmpty(rabbitMqOptions.ExchangeName)
                                                             ? type.Name.Underscore()
                                                             : $"{rabbitMqOptions.ExchangeName}_{type.Name.Underscore()}",
                                                         e =>
                                                         {
                                                             e.UseConsumeFilter(typeof(ConsumeFilter<>),
                                                                                context); //generic filter

                                                             foreach (Type consumer in consumers)
                                                             {
                                                                 configurator.ConfigureEndpoints(context,
                                                                  x => x.Exclude(consumer));
                                                                 MethodInfo methodInfo
                                                                     = typeof(
                                                                             DependencyInjectionReceiveEndpointExtensions)
                                                                         .GetMethods()
                                                                         .Where(x => x.GetParameters()
                                                                                    .Any(p => p.ParameterType ==
                                                                                        typeof(IServiceProvider)))
                                                                         .FirstOrDefault(x => x.Name == "Consumer" &&
                                                                             x.IsGenericMethod);

                                                                 MethodInfo generic
                                                                     = methodInfo?.MakeGenericMethod(consumer);
                                                                 generic?.Invoke(e, new object[] { e, context, null });
                                                             }
                                                         });
                    }
                });
            });

        return services;
    }
}
