using System.Reflection;

using BuildingBlocks.Caching;
using BuildingBlocks.Core;
using BuildingBlocks.EFCore;
using BuildingBlocks.Exception;
using BuildingBlocks.HealthCheck;
using BuildingBlocks.IdsGenerator;
using BuildingBlocks.Jwt;
using BuildingBlocks.Logging;
using BuildingBlocks.Mapster;
using BuildingBlocks.MassTransit;
using BuildingBlocks.MessageProcessor;
using BuildingBlocks.Mongo;
using BuildingBlocks.OpenTelemetry;
using BuildingBlocks.Swagger;
using BuildingBlocks.Web;

using Figgle;

using Flight;
using Flight.Data;
using Flight.Data.Seed;
using Flight.Extensions;

using FluentValidation;

using Hellang.Middleware.ProblemDetails;

using Microsoft.AspNetCore.Mvc.ApiExplorer;

using Prometheus;

using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;
IWebHostEnvironment env = builder.Environment;

AppOptions? appOptions = builder.Services.GetOptions<AppOptions>("AppOptions");
Console.WriteLine(FiggleFonts.Standard.Render(appOptions.Name));

builder.Services.AddCustomDbContext<FlightDbContext>(configuration);
builder.Services.AddScoped<IDataSeeder, FlightDataSeeder>();

builder.Services.AddMongoDbContext<FlightReadDbContext>(configuration);

builder.Services.AddPersistMessage(configuration);

builder.AddCustomSerilog();
builder.Services.AddJwt();
builder.Services.AddControllers();
builder.Services.AddCustomSwagger(configuration, typeof(FlightRoot).Assembly);
builder.Services.AddCustomVersioning();
builder.Services.AddCustomMediatR();
builder.Services.AddValidatorsFromAssembly(typeof(FlightRoot).Assembly);
builder.Services.AddCustomProblemDetails();
builder.Services.AddCustomMapster(typeof(FlightRoot).Assembly);
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IEventMapper, EventMapper>();
builder.Services.AddCustomMassTransit(typeof(FlightRoot).Assembly, env);
builder.Services.AddCustomOpenTelemetry();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddCustomHealthCheck();

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcExceptionInterceptor>();
});

builder.Services.AddMagicOnion();

SnowFlakIdGenerator.Configure(1);

builder.Services.AddCachingRequest(new List<Assembly> { typeof(FlightRoot).Assembly });

builder.Services.AddEasyCaching(options => { options.UseInMemory(configuration, "mem"); });

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    IApiVersionDescriptionProvider? provider = app.Services.GetService<IApiVersionDescriptionProvider>();
    app.UseCustomSwagger(provider);
}

app.UseSerilogRequestLogging();
app.UseCorrelationId();
app.UseRouting();
app.UseHttpMetrics();
app.UseMigration<FlightDbContext>(env);
app.UseProblemDetails();
app.UseHttpsRedirection();
app.UseCustomHealthCheck();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapMetrics();
    endpoints.MapMagicOnionService();
});

app.MapGet("/", x => x.Response.WriteAsync(appOptions.Name));
app.Run();

namespace Flight.Api
{
    public partial class Program
    { }
}
