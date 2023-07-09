using System.Reflection;

using Mapster;

using MapsterMapper;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Mapster;

public static class Extensions
{
    public static IServiceCollection AddCustomMapster(this IServiceCollection services, Assembly assembly)
    {
        TypeAdapterConfig typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
        typeAdapterConfig.Scan(assembly);
        Mapper mapperConfig = new Mapper(typeAdapterConfig);
        services.AddSingleton<IMapper>(mapperConfig);

        return services;
    }
}
