using EasyCaching.Core;

using MediatR;

using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching;

public class InvalidateCachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    private readonly IEasyCachingProvider _cachingProvider;
    private readonly IInvalidateCacheRequest _invalidateCacheRequest;
    private readonly ILogger<InvalidateCachingBehavior<TRequest, TResponse>> _logger;


    public InvalidateCachingBehavior(IEasyCachingProviderFactory cachingFactory,
        ILogger<InvalidateCachingBehavior<TRequest, TResponse>> logger,
        IInvalidateCacheRequest invalidateCacheRequest)
    {
        _logger = logger;
        _cachingProvider = cachingFactory.GetCachingProvider("mem");
        _invalidateCacheRequest = invalidateCacheRequest;
    }

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        if (request is not IInvalidateCacheRequest || _invalidateCacheRequest == null)
            // No cache request found, so just continue through the pipeline
            return await next();

        string cacheKey = _invalidateCacheRequest.CacheKey;
        TResponse response = await next();

        await _cachingProvider.RemoveAsync(cacheKey);

        _logger.LogDebug("Cache data with cache key: {CacheKey} removed.", cacheKey);

        return response;
    }
}
