using System.Diagnostics;

using MediatR;

using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Logging;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        const string prefix = nameof(LoggingBehavior<TRequest, TResponse>);

        _logger.LogInformation("[{Prefix}] Handle request={X-RequestData} and response={X-ResponseData}",
                               prefix, typeof(TRequest).Name, typeof(TResponse).Name);

        Stopwatch timer = new Stopwatch();
        timer.Start();

        TResponse response = await next();

        timer.Stop();
        TimeSpan timeTaken = timer.Elapsed;
        if (timeTaken.Seconds > 3) // if the request is greater than 3 seconds, then log the warnings
            _logger.LogWarning("[{Perf-Possible}] The request {X-RequestData} took {TimeTaken} seconds.",
                               prefix, typeof(TRequest).Name, timeTaken.Seconds);

        _logger.LogInformation("[{Prefix}] Handled {X-RequestData}", prefix, typeof(TRequest).Name);
        return response;
    }
}
