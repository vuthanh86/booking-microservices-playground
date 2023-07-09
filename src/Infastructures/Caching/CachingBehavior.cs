using EasyCaching.Core;

using MediatR;

using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    private readonly ICacheRequest _cacheRequest;
    private readonly IEasyCachingProvider _cachingProvider;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    private readonly int _defaultCacheExpirationInHours = 1;

    public CachingBehavior(IEasyCachingProviderFactory cachingFactory,
        ILogger<CachingBehavior<TRequest, TResponse>> logger,
        ICacheRequest cacheRequest)
    {
        _logger = logger;
        _cachingProvider = cachingFactory.GetCachingProvider("mem");
        _cacheRequest = cacheRequest;
    }


    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        if (request is not ICacheRequest || _cacheRequest == null)
            // No cache request found, so just continue through the pipeline
            return await next();

        string cacheKey = _cacheRequest.CacheKey;
        CacheValue<TResponse> cachedResponse = await _cachingProvider.GetAsync<TResponse>(cacheKey);
        if (cachedResponse.Value != null)
        {
            _logger.LogDebug("Response retrieved {TRequest} from cache. CacheKey: {CacheKey}",
                             typeof(TRequest).FullName, cacheKey);
            return cachedResponse.Value;
        }

        TResponse response = await next();

        DateTime expirationTime = _cacheRequest.AbsoluteExpirationRelativeToNow ??
                                  DateTime.Now.AddHours(_defaultCacheExpirationInHours);

        await _cachingProvider.SetAsync(cacheKey, response, expirationTime.TimeOfDay);

        _logger.LogDebug("Caching response for {TRequest} with cache key: {CacheKey}", typeof(TRequest).FullName,
                         cacheKey);

        return response;
    }
}
