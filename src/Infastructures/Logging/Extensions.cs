using BuildingBlocks.Web;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.SpectreConsole;

namespace BuildingBlocks.Logging;

public static class Extensions
{
    public static WebApplicationBuilder AddCustomSerilog(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        builder.Host.UseSerilog((context, loggerConfiguration) =>
        {
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            LogOptions loggOptions = context.Configuration.GetSection(nameof(LogOptions)).Get<LogOptions>();
            AppOptions appOptions = context.Configuration.GetSection(nameof(AppOptions)).Get<AppOptions>();

            LogEventLevel logLevel = Enum.TryParse(loggOptions.Level, true, out LogEventLevel level)
                                         ? level
                                         : LogEventLevel.Information;

            loggerConfiguration
                .WriteTo.Console()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(loggOptions.ElasticUri))
                {
                    AutoRegisterTemplate = true,
                    IndexFormat
                        = $"{appOptions.Name}-{environment?.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}"
                })
                .WriteTo.SpectreConsole(loggOptions.LogTemplate, logLevel)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
                .Enrich.WithSpan()
                .Enrich.WithExceptionDetails()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(context.Configuration);
        });

        return builder;
    }
}
