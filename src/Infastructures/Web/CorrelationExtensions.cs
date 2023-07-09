using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace BuildingBlocks.Web;

public static class CorrelationExtensions
{
    private const string CorrelationId = "correlationId";

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (ctx, next) =>
        {
            if (!ctx.Request.Headers.TryGetValue(CorrelationId, out StringValues correlationId))
                correlationId = Guid.NewGuid().ToString("N");

            ctx.Items[CorrelationId] = correlationId.ToString();
            await next();
        });
    }

    public static Guid GetCorrelationId(this HttpContext context)
    {
        context.Items.TryGetValue(CorrelationId, out object correlationId);
        return string.IsNullOrEmpty(correlationId?.ToString()) ? Guid.NewGuid() : new Guid(correlationId.ToString()!);
    }
}
