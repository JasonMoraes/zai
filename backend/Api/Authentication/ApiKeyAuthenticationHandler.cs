using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Api.Authentication;

public static class ApiKeyDefaults
{
    public const string AuthenticationScheme = "ApiKey";
    public const string HeaderName = "X-API-Key";
}

/// <summary>
/// Authenticates a sensor by the API key in <c>X-API-Key</c>. The principal carries the sensor and the series it
/// belongs to, so uploads never take the series from the request body.
/// </summary>
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISensorService sensors)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyDefaults.HeaderName, out var values) || values.Count != 1
            || string.IsNullOrEmpty(values[0]))
            return AuthenticateResult.NoResult();

        var sensor = await sensors.FindByApiKeyAsync(values[0]!, Context.RequestAborted);
        if (sensor is null)
            return AuthenticateResult.Fail("Invalid API key.");

        var identity = new ClaimsIdentity(
        [
            new Claim(SensorClaimTypes.SensorId, sensor.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(SensorClaimTypes.SeriesId, sensor.SeriesId.ToString(CultureInfo.InvariantCulture)),
        ], Scheme.Name);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = $"{ApiKeyDefaults.AuthenticationScheme} header=\"{ApiKeyDefaults.HeaderName}\"";
        return base.HandleChallengeAsync(properties);
    }
}
