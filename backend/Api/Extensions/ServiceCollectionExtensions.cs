using Api.Configuration;
using Api.Data;
using Api.Entities;
using Api.Errors;
using Api.OpenApi;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddControllers(options =>
        {
            // Report validation errors under JSON names ("username"), not C# names ("Username").
            options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
            // DTOs declare [Required] explicitly; the implicit rule only adds noise like "The request field is required."
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            // Contract: an Accept header we can't satisfy (e.g. only application/xml) gets 406.
            options.ReturnHttpNotAcceptable = true;
            // Accept request bodies as application/json only (default also takes text/json and application/*+json).
            // Doing this here instead of per-action [Consumes] keeps the 415 check after authorization,
            // so an anonymous request with a wrong body still gets 401 first.
            var jsonInput = options.InputFormatters.OfType<SystemTextJsonInputFormatter>().Single();
            jsonInput.SupportedMediaTypes.Clear();
            jsonInput.SupportedMediaTypes.Add("application/json");
        })
        .ConfigureApiBehaviorOptions(options =>
            options.InvalidModelStateResponseFactory = ValidationProblems.FromModelState)
        // Don't echo System.Text.Json exception text (.NET type names, byte offsets) back to clients;
        // the field is still reported and the message falls back to a neutral one.
        .AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SupportNonNullableReferenceTypes();
            options.NonNullableReferenceTypesAsRequired();
            options.AddSecurityDefinition(BearerAuthOperationFilter.SchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token z `/api/auth/login`.",
            });
            options.OperationFilter<BearerAuthOperationFilter>();
        });

        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        // Resolved lazily so tests can override the connection string; the startup migration still fails fast.
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'Default' is not configured.");

            options.UseNpgsql(connectionString);
        });
        services.AddOptions<AdminOptions>()
            .BindConfiguration(AdminOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = jwt.CreateSigningKey(),
                    ValidAlgorithms = [JwtOptions.Algorithm],
                    NameClaimType = JwtRegisteredClaimNames.UniqueName,
                    ClockSkew = JwtOptions.ClockSkew,
                };
                bearer.Events = new JwtBearerEvents { OnTokenValidated = RejectRevokedTokens };
            });
        services.AddAuthorization();

        return services;
    }

    // A signed, unexpired token is still invalid once logout has put its jti on the denylist.
    private static async Task RejectRevokedTokens(TokenValidatedContext context)
    {
        var jti = context.Principal?.GetTokenId();
        var revocations = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationService>();

        if (jti is null || await revocations.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
            context.Fail("Token has been revoked.");
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenRevocationService, TokenRevocationService>();
        services.AddSingleton<IApiKeyGenerator, ApiKeyGenerator>();
        services.AddScoped<ISensorService, SensorService>();

        return services;
    }
}
