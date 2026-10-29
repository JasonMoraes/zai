using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Extensions;

/// <summary>Reads the claims <see cref="Services.TokenService"/> puts into access tokens (raw JWT names, since MapInboundClaims is off).</summary>
public static class ClaimsPrincipalExtensions
{
    public static string? GetTokenId(this ClaimsPrincipal user) => user.FindFirstValue(JwtRegisteredClaimNames.Jti);

    public static int? GetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    public static DateTimeOffset? GetTokenExpiry(this ClaimsPrincipal user) =>
        long.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Exp), NumberStyles.None, CultureInfo.InvariantCulture, out var exp)
            ? DateTimeOffset.FromUnixTimeSeconds(exp)
            : null;
}
