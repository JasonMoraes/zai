using System.Security.Claims;
using Api.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Api.Features.Auth;

public class TokenService(IOptions<JwtOptions> options, TimeProvider time)
{
    public LoginResponse CreateToken(AdminUser user)
    {
        var jwt = options.Value;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            ]),
            Expires = time.GetUtcNow().AddSeconds(jwt.ExpiresInSeconds).UtcDateTime,
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        };

        return new LoginResponse(new JsonWebTokenHandler().CreateToken(descriptor), "Bearer", jwt.ExpiresInSeconds);
    }
}
