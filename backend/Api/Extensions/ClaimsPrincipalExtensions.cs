using System.Globalization;
using System.Security.Claims;
using Api.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Extensions;

/// <summary>
/// Reads the claims the app issues: admin access tokens from <see cref="Services.TokenService"/> (raw JWT names, since
/// MapInboundClaims is off) and sensor principals from <see cref="ApiKeyAuthenticationHandler"/>.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static string? GetTokenId(this ClaimsPrincipal user) => user.FindFirstValue(JwtRegisteredClaimNames.Jti);

    public static int? GetUserId(this ClaimsPrincipal user) => ParseInt(user.FindFirstValue(JwtRegisteredClaimNames.Sub));

    public static DateTimeOffset? GetTokenExpiry(this ClaimsPrincipal user) =>
        long.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Exp), NumberStyles.None, CultureInfo.InvariantCulture, out var exp)
            ? DateTimeOffset.FromUnixTimeSeconds(exp)
            : null;

    public static int? GetSensorId(this ClaimsPrincipal user) => ParseInt(user.FindFirstValue(SensorClaimTypes.SensorId));

    public static int? GetSeriesId(this ClaimsPrincipal user) => ParseInt(user.FindFirstValue(SensorClaimTypes.SeriesId));

    private static int? ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null;
}
