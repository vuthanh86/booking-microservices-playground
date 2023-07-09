using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.Web;

public static class ConfigurationHelper
{
    public static IConfiguration GetConfiguration(string basePath = null)
    {
        basePath ??= Directory.GetCurrentDirectory();
        string environmentVariable = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile($"appsettings.{environmentVariable}.json", true)
            .AddEnvironmentVariables()
            .Build();
    }
}
