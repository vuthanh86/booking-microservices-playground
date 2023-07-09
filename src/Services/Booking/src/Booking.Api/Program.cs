using Booking;
using Booking.Configuration;
using Booking.Data;
using Booking.Extensions;

using BuildingBlocks.Core;
using BuildingBlocks.EFCore;
using BuildingBlocks.EventStoreDB;
using BuildingBlocks.HealthCheck;
using BuildingBlocks.IdsGenerator;
using BuildingBlocks.Jwt;
using BuildingBlocks.Logging;
using BuildingBlocks.Mapster;
using BuildingBlocks.MassTransit;
using BuildingBlocks.MessageProcessor;
using BuildingBlocks.OpenTelemetry;
using BuildingBlocks.Swagger;
using BuildingBlocks.Web;

using Figgle;

using FluentValidation;

using Hellang.Middleware.ProblemDetails;

using Microsoft.AspNetCore.Mvc.ApiExplorer;

using Prometheus;

using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;
IWebHostEnvironment env = builder.Environment;

AppOptions? appOptions = builder.Services.GetOptions<AppOptions>("AppOptions");
builder.Services.Configure<GrpcOptions>(options => configuration.GetSection("Grpc").Bind(options));

Console.WriteLine(FiggleFonts.Standard.Render(appOptions.Name));

builder.Services.AddCustomDbContext<BookingDbContext>(configuration);
builder.Services.AddPersistMessage(configuration);

builder.AddCustomSerilog();
builder.Services.AddJwt();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCustomSwagger(configuration, typeof(BookingRoot).Assembly);
builder.Services.AddCustomVersioning();
builder.Services.AddCustomMediatR();
builder.Services.AddValidatorsFromAssembly(typeof(BookingRoot).Assembly);
builder.Services.AddCustomProblemDetails();
builder.Services.AddCustomMapster(typeof(BookingRoot).Assembly);
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<IEventMapper, EventMapper>();
builder.Services.AddCustomHealthCheck();
builder.Services.AddCustomMassTransit(typeof(BookingRoot).Assembly, env);
builder.Services.AddCustomOpenTelemetry();
builder.Services.AddTransient<AuthHeaderHandler>();

builder.Services.AddMagicOnionClients();

SnowFlakIdGenerator.Configure(3);

// ref: https://github.com/oskardudycz/EventSourcing.NetCore/tree/main/Sample/EventStoreDB/ECommerce
builder.Services.AddEventStore(configuration, typeof(BookingRoot).Assembly)
    .AddEventStoreDbSubscriptionToAll();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    IApiVersionDescriptionProvider? provider = app.Services.GetService<IApiVersionDescriptionProvider>();
    app.UseCustomSwagger(provider);
}

app.UseSerilogRequestLogging();
app.UseMigration<BookingDbContext>(env);
app.UseCorrelationId();
app.UseRouting();
app.UseHttpMetrics();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseProblemDetails();
app.UseCustomHealthCheck();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapMetrics();
});

app.MapGet("/", x => x.Response.WriteAsync(appOptions.Name));

app.Run();

namespace Booking.Api
{
    public partial class Program
    { }
}
