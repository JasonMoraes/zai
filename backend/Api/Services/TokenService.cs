using System.Security.Claims;
using Api.Configuration;
using Api.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public class TokenService(IOptions<JwtOptions> options, TimeProvider time) : ITokenService
{
    public AccessToken CreateToken(AdminUser user)
    {
        var jwt = options.Value;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            ]),
            Expires = time.GetUtcNow().AddSeconds(jwt.ExpiresInSeconds).UtcDateTime,
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), JwtOptions.Algorithm),
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), jwt.ExpiresInSeconds);
    }
}
