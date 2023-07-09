using FluentValidation;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Validation;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
{
    private readonly IServiceProvider _serviceProvider;
    private IValidator<TRequest> _validator;

    public ValidationBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        _validator = _serviceProvider.GetService<IValidator<TRequest>>();
        if (_validator is null)
            return await next();

        await _validator.HandleValidationAsync(request);

        return await next();
    }
}
