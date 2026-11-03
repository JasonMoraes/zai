using Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.OpenApi;

/// <summary>
/// Marks protected endpoints with the security scheme they accept (the contract's <c>bearerAuth</c> or
/// <c>apiKeyAuth</c>), so only they get Swagger's lock icon and send the right credential.
/// </summary>
public class SecurityRequirementsOperationFilter : IOperationFilter
{
    public const string BearerSchemeName = "bearerAuth";
    public const string ApiKeySchemeName = "apiKeyAuth";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var authorizeData = metadata.OfType<IAuthorizeData>().ToList();
        if (authorizeData.Count == 0 || metadata.OfType<IAllowAnonymous>().Any())
            return;

        var usesApiKey = authorizeData.Any(a => a.AuthenticationSchemes?.Contains(ApiKeyDefaults.AuthenticationScheme) == true);
        var scheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = usesApiKey ? ApiKeySchemeName : BearerSchemeName,
            },
        };
        operation.Security.Add(new OpenApiSecurityRequirement { [scheme] = [] });
    }
}
