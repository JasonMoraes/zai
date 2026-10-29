using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Api.Features.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "";

    [Required]
    public string Audience { get; set; } = "";

    /// <summary>HMAC-SHA256 key; at least 32 characters.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = "";

    [Range(60, 86400)]
    public int ExpiresInSeconds { get; set; } = 3600;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
