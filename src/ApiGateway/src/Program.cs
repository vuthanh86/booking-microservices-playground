using BuildingBlocks.Jwt;
using BuildingBlocks.Logging;
using BuildingBlocks.Web;

using Figgle;

using Microsoft.AspNetCore.Authentication;

using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

AppOptions appOptions = builder.Services.GetOptions<AppOptions>("AppOptions");
Console.WriteLine(FiggleFonts.Standard.Render(appOptions.Name));

builder.AddCustomSerilog();
builder.Services.AddJwt();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("Yarp"));

WebApplication app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCorrelationId();
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapReverseProxy(proxyPipeline =>
    {
        proxyPipeline.Use(async (context, next) =>
        {
            string token = await context.GetTokenAsync("access_token");
            context.Request.Headers["Authorization"] = $"Bearer {token}";

            await next().ConfigureAwait(false);
        });
    });
});

app.MapGet("/", x => x.Response.WriteAsync(appOptions.Name));

app.Run();
