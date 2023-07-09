using Humanizer;

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace BuildingBlocks.Swagger;

public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ApiDescription apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/1752#issue-663991077
        foreach (ApiResponseType responseType in context.ApiDescription.SupportedResponseTypes)
        {
            // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/b7cf75e7905050305b115dd96640ddd6e74c7ac9/src/Swashbuckle.AspNetCore.SwaggerGen/SwaggerGenerator/SwaggerGenerator.cs#L383-L387
            string responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            OpenApiResponse response = operation.Responses[responseKey];

            foreach (string contentType in response.Content.Keys)
                if (responseType.ApiResponseFormats.All(x => x.MediaType != contentType))
                    response.Content.Remove(contentType);
        }

        if (operation.Parameters == null)
            return;

        // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/412
        // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/pull/413
        foreach (OpenApiParameter parameter in operation.Parameters)
        {
            ApiParameterDescription description
                = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

            if (parameter.Description == null)
                parameter.Description = description.ModelMetadata?.Description;

            parameter.Name = description.Name.Camelize();

            if (parameter.Schema.Default == null && description.DefaultValue != null)
            {
                // REF: https://github.com/Microsoft/aspnet-api-versioning/issues/429#issuecomment-605402330
                string json = JsonConvert.SerializeObject(description.DefaultValue, description.ModelMetadata
                                                              .ModelType,
                                                          new JsonSerializerSettings
                                                          {
                                                              ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                                                          });
                parameter.Schema.Default = OpenApiAnyFactory.CreateFromJson(json);
            }

            parameter.Required |= description.IsRequired;
        }
    }
}
