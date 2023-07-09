using BuildingBlocks.Core;
using BuildingBlocks.EFCore;
using BuildingBlocks.HealthCheck;
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

using Identity;
using Identity.Data;
using Identity.Extensions;

using Microsoft.AspNetCore.Mvc.ApiExplorer;

using Prometheus;

using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;
IWebHostEnvironment env = builder.Environment;

AppOptions? appOptions = builder.Services.GetOptions<AppOptions>("AppOptions");
Console.WriteLine(FiggleFonts.Standard.Render(appOptions.Name));

builder.Services.AddCustomDbContext<IdentityContext>(configuration);
builder.Services.AddScoped<IDataSeeder, IdentityDataSeeder>();

builder.Services.AddPersistMessage(configuration);

builder.AddCustomSerilog();
builder.Services.AddControllers();
builder.Services.AddCustomSwagger(configuration, typeof(IdentityRoot).Assembly);
builder.Services.AddCustomVersioning();
builder.Services.AddCustomMediatR();
builder.Services.AddValidatorsFromAssembly(typeof(IdentityRoot).Assembly);
builder.Services.AddCustomProblemDetails();
builder.Services.AddCustomMapster(typeof(IdentityRoot).Assembly);
builder.Services.AddCustomHealthCheck();
builder.Services.AddTransient<IEventMapper, EventMapper>();

builder.Services.AddCustomMassTransit(typeof(IdentityRoot).Assembly, env);
builder.Services.AddCustomOpenTelemetry();

builder.Services.AddIdentityServer(env);

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    IApiVersionDescriptionProvider? provider = app.Services.GetService<IApiVersionDescriptionProvider>();
    app.UseCustomSwagger(provider);
}

app.UseSerilogRequestLogging();
app.UseMigration<IdentityContext>(env);
app.UseCorrelationId();
app.UseRouting();
app.UseHttpMetrics();
app.UseProblemDetails();
app.UseHttpsRedirection();
app.UseCustomHealthCheck();
app.UseAuthentication();
app.UseAuthorization();
app.UseIdentityServer();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapMetrics();
});

app.MapGet("/", x => x.Response.WriteAsync(appOptions.Name));

app.Run();

namespace Identity.Api
{
    public partial class Program
    { }
}
