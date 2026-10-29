using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.OpenApi;

/// <summary>Marks [Authorize] endpoints as requiring the bearer token, so only they get Swagger's lock icon.</summary>
public class BearerAuthOperationFilter : IOperationFilter
{
    public const string SchemeName = "bearerAuth";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (!metadata.OfType<IAuthorizeData>().Any() || metadata.OfType<IAllowAnonymous>().Any())
            return;

        var scheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = SchemeName },
        };
        operation.Security.Add(new OpenApiSecurityRequirement { [scheme] = [] });
    }
}
