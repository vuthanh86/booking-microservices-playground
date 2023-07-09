﻿using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace BuildingBlocks.Swagger;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly SwaggerOptions _options;
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, IOptions<SwaggerOptions> options)
    {
        _provider = provider;
        _options = options.Value;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (ApiVersionDescription description in _provider.ApiVersionDescriptions)
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
    }

    private OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        OpenApiInfo info = new OpenApiInfo
        {
            Title = _options.Title ?? "APIs",
            Version = _options.Version ?? description.ApiVersion.ToString(),
            Description = "An application with Swagger, Swashbuckle, and API versioning.",
            Contact = new OpenApiContact { Name = "", Email = "" },
            License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
        };

        if (description.IsDeprecated)
            info.Description += " This API version has been deprecated.";

        return info;
    }
}
