using System;

using BuildingBlocks.Exception;

using Hellang.Middleware.ProblemDetails;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Identity.Extensions;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddCustomProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(x =>
        {
            // Control when an exception is included
            x.IncludeExceptionDetails = (ctx, _) =>
            {
                // Fetch services from HttpContext.RequestServices
                IHostEnvironment env = ctx.RequestServices.GetRequiredService<IHostEnvironment>();
                return env.IsDevelopment() || env.IsStaging();
            };
            x.Map<ConflictException>(ex => new ProblemDetailsWithCode
            {
                Title = "Application rule broken",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message,
                Type = "https://somedomain/application-rule-validation-error"
            });

            // Exception will produce and returns from our FluentValidation RequestValidationBehavior
            x.Map<ValidationException>(ex => new ProblemDetailsWithCode
            {
                Title = "input validation rules broken",
                Status = (int)ex.StatusCode,
                Detail = ex.Message,
                Type = "https://somedomain/input-validation-rules-error"
            });
            x.Map<BadRequestException>(ex => new ProblemDetailsWithCode
            {
                Title = "bad request exception",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
                Type = "https://somedomain/bad-request-error"
            });
            x.Map<NotFoundException>(ex => new ProblemDetailsWithCode
            {
                Title = "not found exception",
                Status = StatusCodes.Status404NotFound,
                Detail = ex.Message,
                Type = "https://somedomain/not-found-error"
            });
            x.Map<InternalServerException>(ex => new ProblemDetailsWithCode
            {
                Title = "api server exception",
                Status = StatusCodes.Status500InternalServerError,
                Detail = ex.Message,
                Type = "https://somedomain/api-server-error"
            });
            x.Map<AppException>(ex => new ProblemDetailsWithCode
            {
                Title = "application exception",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
                Type = "https://somedomain/application-error"
            });

            x.MapToStatusCode<ArgumentNullException>(StatusCodes.Status400BadRequest);

            x.MapStatusCode = context =>
            {
                return context.Response.StatusCode switch
                {
                    StatusCodes.Status401Unauthorized => new ProblemDetailsWithCode
                    {
                        Status = context.Response.StatusCode,
                        Title = "identity exception",
                        Detail = "You are not Authorized",
                        Type = "https://somedomain/identity-error"
                    },

                    _ => new StatusCodeProblemDetails(context.Response.StatusCode)
                };
            };
        });
        return services;
    }
}
